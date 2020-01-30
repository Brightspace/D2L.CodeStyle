using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.UnnecessaryParameters {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal partial class UnnecessaryParametersAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ParametersShouldBeRemoved
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			IImmutableSet<ISymbol> methods = GetMethods( compilation );

			context.RegisterSyntaxNodeAction(
					ctxt => {
						if( ctxt.Node is InvocationExpressionSyntax invocation ) {
							AnalyzeMethodInvocation( ctxt, invocation, methods );
						}
					},
					SyntaxKind.InvocationExpression
				);
		}

		private static IImmutableSet<ISymbol> GetMethods( Compilation compilation ) {

			ImmutableHashSet<ISymbol>.Builder builder = ImmutableHashSet.CreateBuilder<ISymbol>();

			foreach( KeyValuePair<string, ImmutableArray<string>> pairs in Methods.Definitions ) {

				INamedTypeSymbol type = compilation.GetTypeByMetadataName( pairs.Key );
				if( type != null ) {

					foreach( string name in pairs.Value ) {

						IEnumerable<ISymbol> methods = type
							.GetMembers( name )
							.Where( 
                                m => ( m.Kind == SymbolKind.Method ) 
                            );

						foreach( ISymbol method in methods ) {
							builder.Add( method );
						}
					}
				}
			}

			return builder.ToImmutableHashSet();
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				IImmutableSet<ISymbol> methods
			) {

			ISymbol methodSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression )
				.Symbol;

			if( !AnalyzeMethod( context, methodSymbol, methods, invocation ) ) {
				return;
			}

			ReportDiagnostic( context, methodSymbol, Diagnostics.ParametersShouldBeRemoved );
		}

		private bool AnalyzeMethod(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				IImmutableSet<ISymbol> methods,
				InvocationExpressionSyntax invocation
			) {

			if( memberSymbol.IsNullOrErrorType() ) {
				return false;
			}

			if( !IsMethodSymbol( memberSymbol, methods ) ) {
				return false;
			}

			if( !isParametersRemoved( memberSymbol, invocation ) ) {
				return false;
			}

			return true;
		}

		private static bool IsMethodSymbol(
			ISymbol memberSymbol,
			IImmutableSet<ISymbol> methods
		) {

			ISymbol originalDefinition = memberSymbol.OriginalDefinition;

			if( methods.Contains( originalDefinition ) ) {
				return true;
			}

			return false;
		}

		private void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				DiagnosticDescriptor diagnosticDescriptor
			) {

			Location location = context.ContainingSymbol.Locations[0];
			string methodName = memberSymbol.ToDisplayString( MemberDisplayFormat );

			var diagnostic = Diagnostic.Create(
					diagnosticDescriptor,
					location,
					methodName
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static bool isParametersRemoved(
				ISymbol memberSymbol,
				InvocationExpressionSyntax invocation
			) {

			ArgumentListSyntax argumentList = invocation.ArgumentList;

			switch( memberSymbol.MetadataName) {
				case "HasPermission":
					if ( argumentList.Arguments.Count == 5 ) {
						return true;
					}
					break;
				case "HasCapability":
					if( argumentList.Arguments.Count == 6 ) {
						return true;
					}
					break;
				default:
					break;
			}

			return false;
		}

		private static readonly SymbolDisplayFormat MemberDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);
	}
}
