using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class DangerousMemberUsagesAnalyzer : DiagnosticAnalyzer {

		private const string DangerousMethodAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+AuditedAttribute";
		private const string DangerousMethodUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+UnauditedAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousMethodsShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( DangerousMethodAuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( DangerousMethodUnauditedAttributeFullName );
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

			if( !AnalyzePotentiallyDangerousMember( context, methodSymbol, auditedAttributeType, unauditedAttributeType, dangerousMethods ) ) {
				return;
			}

			ReportDiagnostic( context, methodSymbol, Diagnostics.DangerousMethodsShouldBeAvoided );
		}

		private bool AnalyzePotentiallyDangerousMember(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousMembers
			) {

			if( memberSymbol.IsNullOrErrorType() ) {
				return false;
			}

			if( !IsDangerousMemberSymbol( memberSymbol, dangerousMembers ) ) {
				return false;
			}

			bool isAudited = context.ContainingSymbol
				.GetAttributes()
				.Any( attr => IsAuditedAttribute( auditedAttributeType, unauditedAttributeType, attr, memberSymbol ) );

			if( isAudited ) {
				return false;
			}

			return true;
		}

		private static bool IsDangerousMemberSymbol(
				ISymbol memberSymbol,
				IImmutableSet<ISymbol> dangerousMembers
			) {

			ISymbol originalDefinition = memberSymbol.OriginalDefinition;

			if( dangerousMembers.Contains( originalDefinition ) ) {
				return true;
			}

			if( !ReferenceEquals( memberSymbol, originalDefinition ) ) {

				if( dangerousMembers.Contains( memberSymbol ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsAuditedAttribute(
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				AttributeData attr,
				ISymbol memberSymbol
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
			if( !memberSymbol.ContainingType.Equals( typeArg.Value ) ) {
				return false;
			}

			TypedConstant nameArg = attr.ConstructorArguments[ 1 ];
			if( nameArg.Value == null ) {
				return false;
			}
			if( !memberSymbol.Name.Equals( nameArg.Value ) ) {
				return false;
			}

			return true;
		}

		private void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				DiagnosticDescriptor diagnosticDescriptor
			) {

			Location location = context.ContainingSymbol.Locations[ 0 ];
			string methodName = memberSymbol.ToDisplayString( MemberDisplayFormat );

			var diagnostic = Diagnostic.Create(
					diagnosticDescriptor,
					location,
					methodName
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static readonly SymbolDisplayFormat MemberDisplayFormat = new SymbolDisplayFormat(
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
