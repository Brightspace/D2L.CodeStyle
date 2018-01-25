using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMethodUsages {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class DangerousMethodUsagesAnalyzer : DiagnosticAnalyzer {

		private const string AuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+AuditedAttribute";
		private const string UnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+UnauditedAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousMethodsShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( AuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( UnauditedAttributeFullName );
			IImmutableSet<ISymbol> dangerousMethods = GetDangerousMethods( compilation );

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeMethod( ctxt, auditedAttributeType, unauditedAttributeType, dangerousMethods ),
					SyntaxKind.InvocationExpression
				);
		}

		private void AnalyzeMethod(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousMethods
			) {

			InvocationExpressionSyntax invocation = ( context.Node as InvocationExpressionSyntax );
			if( invocation != null ) {
				AnalyzeInnovation( context, invocation, auditedAttributeType, unauditedAttributeType, dangerousMethods );
			}
		}

		private void AnalyzeInnovation(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousMethods
			) {

			ISymbol methodSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression )
				.Symbol;

			if( methodSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !IsDangerousMethodSymbol( methodSymbol, dangerousMethods ) ) {
				return;
			}

			bool isAudited = context.ContainingSymbol
				.GetAttributes()
				.Any( attr => IsAuditedAttribute( auditedAttributeType, unauditedAttributeType, attr, methodSymbol ) );

			if( isAudited ) {
				return;
			}

			ReportDiagnostic( context, methodSymbol );
		}

		private static bool IsDangerousMethodSymbol(
				ISymbol methodSymbol,
				IImmutableSet<ISymbol> dangerousMethods
			) {

			ISymbol originalDefinition = methodSymbol.OriginalDefinition;

			if( dangerousMethods.Contains( originalDefinition ) ) {
				return true;
			}

			if( !ReferenceEquals( methodSymbol, originalDefinition ) ) {

				if( dangerousMethods.Contains( methodSymbol ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsAuditedAttribute(
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				AttributeData attr,
				ISymbol methodSymbol
			) {

			bool isAudited = (
					attr.AttributeClass.Equals( auditedAttributeType )
					|| attr.AttributeClass.Equals( unauditedAttributeType )
				);
			if( !isAudited ) {
				return false;
			}

			if( attr.ConstructorArguments.Length < 2 ) {
				return false;
			}

			TypedConstant typeArg = attr.ConstructorArguments[ 0 ];
			if( typeArg.Value == null ) {
				return false;
			}
			if( !methodSymbol.ContainingType.Equals( typeArg.Value ) ) {
				return false;
			}

			TypedConstant nameArg = attr.ConstructorArguments[ 1 ];
			if( nameArg.Value == null ) {
				return false;
			}
			if( !methodSymbol.Name.Equals( nameArg.Value ) ) {
				return false;
			}

			return true;
		}

		private void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				ISymbol methodSymbol
			) {

			Location location = context.ContainingSymbol.Locations[ 0 ];
			string methodName = methodSymbol.ToDisplayString( MethodDisplayFormat );

			var diagnostic = Diagnostic.Create(
					Diagnostics.DangerousMethodsShouldBeAvoided,
					location,
					methodName
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static readonly SymbolDisplayFormat MethodDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

		private static IImmutableSet<ISymbol> GetDangerousMethods( Compilation compilation ) {

			ImmutableHashSet<ISymbol>.Builder builder = ImmutableHashSet.CreateBuilder<ISymbol>();

			foreach( KeyValuePair<string, ImmutableArray<string>> pairs in DangerousMethods.Definitions ) {

				INamedTypeSymbol type = compilation.GetTypeByMetadataName( pairs.Key );
				if( type != null ) {

					foreach( string name in pairs.Value ) {

						IEnumerable<ISymbol> methods = type
							.GetMembers( name )
							.Where( m => (
								m.Kind == SymbolKind.Method
								|| m.Kind == SymbolKind.Property
							) );

						foreach( ISymbol method in methods ) {
							builder.Add( method );
						}
					}
				}
			}

			return builder.ToImmutableHashSet();
		}
	}
}
