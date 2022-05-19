using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ConsistentParameterAttributesAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.InconsistentMethodAttributeApplication
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		public static void OnCompilationStart( CompilationStartAnalysisContext context ) {
			if( !ConsistentAttributesContext.TryCreate( context.Compilation, out ConsistentAttributesContext? consistentAttributesContext ) ) {
				return;
			}

			context.RegisterSymbolAction(
				ctx => AnalyzeMethodDeclarationConsistency(
					ctx,
					consistentAttributesContext!,
					(IMethodSymbol)ctx.Symbol
				),
				SymbolKind.Method
			);
		}

		private static void AnalyzeMethodDeclarationConsistency(
			SymbolAnalysisContext ctx,
			ConsistentAttributesContext consistentAttributesContext,
			IMethodSymbol methodSymbol
		) {
			// Static methods can't implement interface methods
			if( methodSymbol.IsStatic ) {
				return;
			}

			ImmutableArray<IMethodSymbol> implementedMethods = methodSymbol.GetImplementedMethods();

			if( implementedMethods.IsEmpty ) {
				return;
			}

			var methodUsage = methodSymbol
				.Parameters
				.Select( consistentAttributesContext.GetAttributeUsage )
				.ToImmutableArray();

			foreach( IMethodSymbol implementedMethod in implementedMethods ) {
				for( int i = 0; i < methodSymbol.Parameters.Length; ++i ) {
					var thisParameterUsage = methodUsage[ i ];
					var implementedParameterUsage = consistentAttributesContext.GetAttributeUsage( implementedMethod.Parameters[ i ] );

					for( int j = 0; j < thisParameterUsage.Length; ++j ) {
						var thisParameterUsageAttr = thisParameterUsage[ j ];
						var implementedParameterUsageAttr = implementedParameterUsage[ j ];

						if( thisParameterUsageAttr != implementedParameterUsageAttr ) {
							ctx.ReportDiagnostic(
								Diagnostics.InconsistentMethodAttributeApplication,
								GetLocationOfNthParameter( methodSymbol, i, ctx.CancellationToken ),
								messageArgs: new[] {
									thisParameterUsageAttr.AttributeName,
									$"{ methodSymbol.ContainingType.Name }.{ methodSymbol.Name }",
									$"{ implementedMethod.ContainingType.Name }.{ implementedMethod.Name }"
								}
							);
						}
					}
				}
			}
		}
		private static Location GetLocationOfNthParameter(
				IMethodSymbol methodSymbol,
				int N,
				CancellationToken cancellationToken
			) {

			MethodDeclarationSyntax? syntax = methodSymbol
				.DeclaringSyntaxReferences[ 0 ]
				.GetSyntax( cancellationToken ) as MethodDeclarationSyntax;

			Location loc = syntax!.ParameterList.Parameters[ N ].GetLocation();
			return loc;
		}

		private sealed class ConsistentAttributesContext {

			private readonly INamedTypeSymbol m_constantAttribute;
			private readonly INamedTypeSymbol m_statelessFuncAttribute;

			private ConsistentAttributesContext(
				INamedTypeSymbol constantAttribute,
				INamedTypeSymbol statelessFuncAttribute
			) {
				m_constantAttribute = constantAttribute;
				m_statelessFuncAttribute = statelessFuncAttribute;
			}

			public static bool TryCreate(
				Compilation compilation,
				out ConsistentAttributesContext? consistentAttributesContext
			) {
				INamedTypeSymbol? constantAttribute = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Contract.ConstantAttribute" );
				if( constantAttribute.IsNullOrErrorType() ) {
					consistentAttributesContext = null;
					return false;
				}

				INamedTypeSymbol? statelessFuncAttribute = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Contract.StatelessFuncAttribute" );
				if( statelessFuncAttribute.IsNullOrErrorType() ) {
					consistentAttributesContext = null;
					return false;
				}

				consistentAttributesContext = new(
					constantAttribute: constantAttribute,
					statelessFuncAttribute: statelessFuncAttribute
				);
				return true;
			}

			public ImmutableArray<(string AttributeName, bool Applied)> GetAttributeUsage(
				ISymbol symbol
			) => ImmutableArray.Create(
				("Constant", HasAttribute( symbol, m_constantAttribute )),
				("StatelessFunc", HasAttribute( symbol, m_statelessFuncAttribute ))
			);

			internal static bool HasAttribute( ISymbol s, INamedTypeSymbol attribute )
				=> s.GetAttributes().Any( a => SymbolEqualityComparer.Default.Equals( a.AttributeClass, attribute ) );
		}
	}
}
