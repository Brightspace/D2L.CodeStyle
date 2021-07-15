using System;
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

		private const string DangerousPropertyAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+AuditedAttribute";
		private const string DangerousPropertyUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+UnauditedAttribute";

		private const string DangerousMethodFullName = "D2L.CodeStyle.Annotations.Objects+DangerousMethod";
		private const string DangerousPropertyFullName = "D2L.CodeStyle.Annotations.Objects+DangerousProperty";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousMethodsShouldBeAvoided,
			Diagnostics.DangerousPropertiesShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			RegisterDangerousMethodAnalysis( context );
			RegisterDangerousPropertyAnalysis( context );
		}

		private void RegisterDangerousMethodAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( DangerousMethodAuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( DangerousMethodUnauditedAttributeFullName );
			INamedTypeSymbol dangerousMemberType = compilation.GetTypeByMetadataName( DangerousMethodFullName );
			IImmutableSet<ISymbol> dangerousMethods = GetDangerousMethods( compilation );

			context.RegisterSyntaxNodeAction(
					ctxt => {
						if( ctxt.Node is InvocationExpressionSyntax invocation ) {
							AnalyzeMethodInvocation( ctxt, invocation, auditedAttributeType, unauditedAttributeType, dangerousMemberType, dangerousMethods );
						}
					},
					SyntaxKind.InvocationExpression
				);
		}

		private void RegisterDangerousPropertyAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( DangerousPropertyAuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( DangerousPropertyUnauditedAttributeFullName );
			INamedTypeSymbol dangerousMemberType = compilation.GetTypeByMetadataName( DangerousPropertyFullName );
			IImmutableSet<ISymbol> dangerousProperties = GetDangerousProperties( compilation );

			context.RegisterSyntaxNodeAction(
					ctxt => {
						if( ctxt.Node is MemberAccessExpressionSyntax propertyAccess ) {
							AnalyzePropertyAccess( ctxt, propertyAccess, auditedAttributeType, unauditedAttributeType, dangerousMemberType, dangerousProperties );
						}
					},
					SyntaxKind.SimpleMemberAccessExpression
				);
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				INamedTypeSymbol dangerousMemberType,
				IImmutableSet<ISymbol> dangerousMethods
			) {

			ISymbol methodSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression )
				.Symbol;

			if( !AnalyzePotentiallyDangerousMember( context, methodSymbol, auditedAttributeType, unauditedAttributeType, dangerousMemberType, dangerousMethods ) ) {
				return;
			}

			ReportDiagnostic( context, methodSymbol, Diagnostics.DangerousMethodsShouldBeAvoided );
		}

		private void AnalyzePropertyAccess(
				SyntaxNodeAnalysisContext context,
				MemberAccessExpressionSyntax propertyAccess,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				INamedTypeSymbol dangerousMemberType,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			ISymbol propertySymbol = context.SemanticModel
				.GetSymbolInfo( propertyAccess )
				.Symbol;

			if( !AnalyzePotentiallyDangerousMember( context, propertySymbol, auditedAttributeType, unauditedAttributeType, dangerousMemberType, dangerousProperties ) ) {
				return;
			}

			ReportDiagnostic( context, propertySymbol, Diagnostics.DangerousPropertiesShouldBeAvoided );
		}

		private bool AnalyzePotentiallyDangerousMember(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				INamedTypeSymbol dangerousMemberType,
				IImmutableSet<ISymbol> dangerousMembers
			) {

			if( memberSymbol.IsNullOrErrorType() ) {
				return false;
			}

			if( !IsDangerousMemberSymbol( memberSymbol, dangerousMembers ) && !IsDangerousMemberAttribute( memberSymbol, dangerousMemberType ) ) {
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

		private bool IsDangerousMemberAttribute( ISymbol memberSymbol, INamedTypeSymbol dangerousMemberType ) {
			return memberSymbol.GetAttributes().Any( attr => attr.AttributeClass.Equals( dangerousMemberType, SymbolEqualityComparer.Default ) );
		}

		private static bool IsDangerousMemberSymbol(
				ISymbol memberSymbol,
				IImmutableSet<ISymbol> dangerousMembers
			) {

			ISymbol originalDefinition = memberSymbol.OriginalDefinition;

			if( dangerousMembers.Contains( originalDefinition ) ) {
				return true;
			}

			if( !memberSymbol.Equals( originalDefinition, SymbolEqualityComparer.Default ) ) {

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
					attr.AttributeClass.Equals( auditedAttributeType, SymbolEqualityComparer.Default )
					|| attr.AttributeClass.Equals( unauditedAttributeType, SymbolEqualityComparer.Default )
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
