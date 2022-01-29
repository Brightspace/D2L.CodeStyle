using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class DangerousMemberUsagesAnalyzer : DiagnosticAnalyzer {

		private const string DangerousMethodAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+AuditedAttribute";
		private const string DangerousMethodUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+UnauditedAttribute";

		private const string DangerousPropertyAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+AuditedAttribute";
		private const string DangerousPropertyUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+UnauditedAttribute";

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
			IImmutableSet<ISymbol> dangerousMethods = GetDangerousMethods( compilation );

			context.RegisterOperationAction(
					ctxt => {
						IInvocationOperation invocation = (IInvocationOperation)ctxt.Operation;
						AnalyzeMethod( ctxt, invocation.TargetMethod, auditedAttributeType, unauditedAttributeType, dangerousMethods );
					},
					OperationKind.Invocation
				);

			context.RegisterOperationAction(
					ctxt => {
						IMethodReferenceOperation operation = (IMethodReferenceOperation)ctxt.Operation;
						AnalyzeMethod( ctxt, operation.Method, auditedAttributeType, unauditedAttributeType, dangerousMethods );
					},
					OperationKind.MethodReference
				);
		}

		private void RegisterDangerousPropertyAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol auditedAttributeType = compilation.GetTypeByMetadataName( DangerousPropertyAuditedAttributeFullName );
			INamedTypeSymbol unauditedAttributeType = compilation.GetTypeByMetadataName( DangerousPropertyUnauditedAttributeFullName );
			IImmutableSet<ISymbol> dangerousProperties = GetDangerousProperties( compilation );

			context.RegisterOperationAction(
					ctxt => {
						IPropertyReferenceOperation propertyReference = (IPropertyReferenceOperation)ctxt.Operation;
						AnalyzePropertyReference( ctxt, propertyReference, auditedAttributeType, unauditedAttributeType, dangerousProperties );
					},
					OperationKind.PropertyReference
				);
		}

		private void AnalyzeMethod(
				OperationAnalysisContext context,
				IMethodSymbol method,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousMethods
			) {

			if( !AnalyzePotentiallyDangerousMember( context, method, auditedAttributeType, unauditedAttributeType, dangerousMethods ) ) {
				return;
			}

			ReportDiagnostic( context, method, Diagnostics.DangerousMethodsShouldBeAvoided );
		}

		private void AnalyzePropertyReference(
				OperationAnalysisContext context,
				IPropertyReferenceOperation propertyReference,
				INamedTypeSymbol auditedAttributeType,
				INamedTypeSymbol unauditedAttributeType,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			IPropertySymbol property = propertyReference.Property;

			if( !AnalyzePotentiallyDangerousMember( context, property, auditedAttributeType, unauditedAttributeType, dangerousProperties ) ) {
				return;
			}

			ReportDiagnostic( context, property, Diagnostics.DangerousPropertiesShouldBeAvoided );
		}

		private bool AnalyzePotentiallyDangerousMember(
				OperationAnalysisContext context,
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
				OperationAnalysisContext context,
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

			ImmutableHashSet<ISymbol>.Builder builder = ImmutableHashSet.CreateBuilder( SymbolEqualityComparer.Default );

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

			return builder.ToImmutable();
		}

		private static IImmutableSet<ISymbol> GetDangerousProperties( Compilation compilation ) {

			ImmutableHashSet<ISymbol>.Builder builder = ImmutableHashSet.CreateBuilder( SymbolEqualityComparer.Default );

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

			return builder.ToImmutable();
		}
	}
}
