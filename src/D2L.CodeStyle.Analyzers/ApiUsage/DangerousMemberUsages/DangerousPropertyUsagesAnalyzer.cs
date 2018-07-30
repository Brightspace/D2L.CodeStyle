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
	internal sealed class DangerousPropertyUsagesAnalyzer : DiagnosticAnalyzer {

		private const string AuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+AuditedAttribute";
		private const string UnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+UnauditedAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousPropertiesShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( AuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( UnauditedAttributeFullName );
			IImmutableSet<ISymbol> dangerousProperties = GetDangerousProperties( compilation );

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeProperty( ctxt, auditedAttributeType, unauditedAttributeType, dangerousProperties ),
					SyntaxKind.SimpleMemberAccessExpression
				);
		}

		private void AnalyzeProperty(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			if( context.Node is MemberAccessExpressionSyntax propertyAccess ) {
				AnalyzeMemberAccess( context, propertyAccess, auditedAttributeType, unauditedAttributeType, dangerousProperties );
			}
		}

		private void AnalyzeMemberAccess(
				SyntaxNodeAnalysisContext context,
				MemberAccessExpressionSyntax propertyAccess,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			ISymbol propertySymbol = context.SemanticModel
				.GetSymbolInfo( propertyAccess )
				.Symbol;

			if( propertySymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !IsDangerousPropertySymbol( propertySymbol, dangerousProperties ) ) {
				return;
			}

			bool isAudited = context.ContainingSymbol
				.GetAttributes()
				.Any( attr => IsAuditedAttribute( auditedAttributeType, unauditedAttributeType, attr, propertySymbol ) );

			if( isAudited ) {
				return;
			}

			ReportDiagnostic( context, propertySymbol );
		}

		private static bool IsDangerousPropertySymbol(
				ISymbol propertySymbol,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			ISymbol originalDefinition = propertySymbol.OriginalDefinition;

			if( dangerousProperties.Contains( originalDefinition ) ) {
				return true;
			}

			if( !ReferenceEquals( propertySymbol, originalDefinition ) ) {

				if( dangerousProperties.Contains( propertySymbol ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsAuditedAttribute(
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				AttributeData attr,
				ISymbol propertySymbol
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
			if( !propertySymbol.ContainingType.Equals( typeArg.Value ) ) {
				return false;
			}

			TypedConstant nameArg = attr.ConstructorArguments[ 1 ];
			if( nameArg.Value == null ) {
				return false;
			}
			if( !propertySymbol.Name.Equals( nameArg.Value ) ) {
				return false;
			}

			return true;
		}

		private void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				ISymbol propertySymbol
			) {

			Location location = context.ContainingSymbol.Locations[ 0 ];
			string propertyName = propertySymbol.ToDisplayString( PropertyDisplayFormat );

			var diagnostic = Diagnostic.Create(
					Diagnostics.DangerousPropertiesShouldBeAvoided,
					location,
					propertyName
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static readonly SymbolDisplayFormat PropertyDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

		private static IImmutableSet<ISymbol> GetDangerousProperties( Compilation compilation ) {

			ImmutableHashSet<ISymbol>.Builder builder = ImmutableHashSet.CreateBuilder<ISymbol>();

			foreach( KeyValuePair<string, ImmutableArray<string>> pairs in DangerousProperties.Definitions ) {

				INamedTypeSymbol type = compilation.GetTypeByMetadataName( pairs.Key );
				if( type != null ) {

					foreach( string name in pairs.Value ) {

						IEnumerable<ISymbol> properties = type
							.GetMembers( name )
							.Where( m => ( m.Kind == SymbolKind.Property ) );

						foreach( ISymbol property in properties ) {
							builder.Add( property );
						}
					}
				}
			}

			return builder.ToImmutableHashSet();
		}
	}
}
