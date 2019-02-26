using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ContentFilePhysicalPaths {

	internal sealed class ContentFilePhysicalPathPropertyAnalysis {

		internal static readonly DiagnosticDescriptor DiagnosticDescriptor = Diagnostics.ContentFilePhysicalPathUsages;
		private const string DangerousPropertyName = "PhysicalPath";

		private readonly string m_dangerousTypeName;
		private readonly IImmutableSet<string> m_whitelistedTypes;

		public ContentFilePhysicalPathPropertyAnalysis( 
				string dangerousTypeName, 
				IImmutableSet<string> whitelistedTypes 
			) {

			m_dangerousTypeName = dangerousTypeName;
			m_whitelistedTypes = whitelistedTypes;
		}

		public void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( Register );
		}

		private void Register( CompilationStartAnalysisContext context ) {

			IImmutableSet<ISymbol> dangerousProperties = GetDangerousPropertySymbols( context.Compilation );

			if( dangerousProperties.Count == 0 ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctxt => {
					if( ctxt.Node is MemberAccessExpressionSyntax propertyAccess ) {
						AnalyzePropertyAccess(
								ctxt,
								propertyAccess,
								dangerousProperties
							);
					}
				},
				SyntaxKind.SimpleMemberAccessExpression
			);
		}

		private void AnalyzePropertyAccess(
				SyntaxNodeAnalysisContext context,
				MemberAccessExpressionSyntax propertyAccess,
				IImmutableSet<ISymbol> dangerousProperties
			) {

			ISymbol propertySymbol = ModelExtensions
				.GetSymbolInfo( context.SemanticModel, propertyAccess )
				.Symbol;

			if( propertySymbol.IsNullOrErrorType() ) {
				return;
			}

			if( propertySymbol.Kind != SymbolKind.Property ) {
				return;
			}

			bool isDangerousProperty = IsDangerousMemberSymbol( propertySymbol, dangerousProperties );

			if( !isDangerousProperty ) {
				return;
			}

			bool isWhitelistedType = m_whitelistedTypes.Contains( context.ContainingSymbol.ContainingType.ToString() );
			if( isWhitelistedType ) {
				return;
			}

			ReportDiagnostic( context, propertySymbol, DiagnosticDescriptor );
		}

		private bool IsDangerousMemberSymbol(
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

			// handles explicit implementations, accessed via concrete type;
			// ( i.e. determines whether the member symbol under analysis implements a dangerous interface member )
			foreach( ISymbol dm in dangerousMembers ) {

				ISymbol implementation = memberSymbol.ContainingType.FindImplementationForInterfaceMember( dm );
				if( implementation == null ) {
					// does not implement the interface containing the dangerous property
					continue;
				}

				if( implementation.Equals( memberSymbol ) ) {
					return true;
				}
			}

			return false;
		}

		private void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				ISymbol memberSymbol,
				DiagnosticDescriptor diagnosticDescriptor
			) {

			Location location = context.Node.GetLocation();
			string memberName = memberSymbol.ToDisplayString( MemberDisplayFormat );
			Diagnostic diagnostic = Diagnostic.Create(
					diagnosticDescriptor,
					location,
					memberName
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static readonly SymbolDisplayFormat MemberDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

		private IImmutableSet<ISymbol> GetDangerousPropertySymbols( Compilation compilation ) {

			INamedTypeSymbol type = compilation.GetTypeByMetadataName( m_dangerousTypeName );
			if( type == null ) {
				return ImmutableHashSet<ISymbol>.Empty;
			}

			IImmutableSet<ISymbol> properties = type
				.GetMembers( DangerousPropertyName )
				.Where( m => m.Kind == SymbolKind.Property )
				.ToImmutableHashSet();

			return properties;
		}
	}
}
