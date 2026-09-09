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
						(string AttributeName, ImmutableArray<AttributeData> Instances) thisParameterUsageAttr = thisParameterUsage[ j ];
						(string AttributeName, ImmutableArray<AttributeData> Instances) implementedParameterUsageAttr = implementedParameterUsage[ j ];

						if( thisParameterUsageAttr.Instances.IsDefaultOrEmpty && implementedParameterUsageAttr.Instances.IsDefaultOrEmpty ) {
							continue;
						}

						if( thisParameterUsageAttr.Instances.SequenceEqual( implementedParameterUsageAttr.Instances, AttributeDataComparer.Instance ) ) {
							continue;
						}

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
			private readonly INamedTypeSymbol m_patternStringAttribute;

			private ConsistentAttributesContext(
				INamedTypeSymbol constantAttribute,
				INamedTypeSymbol statelessFuncAttribute,
				INamedTypeSymbol patternStringAttribute
			) {
				m_constantAttribute = constantAttribute;
				m_statelessFuncAttribute = statelessFuncAttribute;
				m_patternStringAttribute = patternStringAttribute;
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

				INamedTypeSymbol? patternStringAttribute = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Contract.PatternStringAttribute" );
				if( patternStringAttribute.IsNullOrErrorType() ) {
					consistentAttributesContext = null;
					return false;
				}

				consistentAttributesContext = new(
					constantAttribute: constantAttribute,
					statelessFuncAttribute: statelessFuncAttribute,
					patternStringAttribute: patternStringAttribute
				);
				return true;
			}

			public ImmutableArray<(string AttributeName, ImmutableArray<AttributeData> Instances)> GetAttributeUsage(
				ISymbol symbol
			) => ImmutableArray.Create(
				("Constant", GetAttributes( symbol, m_constantAttribute )),
				("StatelessFunc", GetAttributes( symbol, m_statelessFuncAttribute )),
				("PatternString", GetAttributes( symbol, m_patternStringAttribute ))
			);

			internal static ImmutableArray<AttributeData> GetAttributes( ISymbol s, INamedTypeSymbol attribute )
				=> s.GetAttributes().Where( a => SymbolEqualityComparer.Default.Equals( a.AttributeClass, attribute ) ).ToImmutableArray();
		}

		private sealed class AttributeDataComparer : IEqualityComparer<AttributeData> {

			public static readonly AttributeDataComparer Instance = new();

			private AttributeDataComparer() { }

			bool IEqualityComparer<AttributeData>.Equals( AttributeData x, AttributeData y ) {
				if( x.ConstructorArguments.Length != y.ConstructorArguments.Length ) {
					return false;
				}

				if( x.NamedArguments.Length != y.NamedArguments.Length ) {
					return false;
				}

				for( int i = 0; i < x.ConstructorArguments.Length; i++ ) {
					if( !x.ConstructorArguments[ i ].Equals( y.ConstructorArguments[ i ] ) ) {
						return false;
					}
				}

				var namedXArgs = x.NamedArguments.OrderBy( static arg => arg.Key, StringComparer.Ordinal ).ToArray();
				var namedYArgs = y.NamedArguments.OrderBy( static arg => arg.Key, StringComparer.Ordinal ).ToArray();
				for( int i = 0; i < namedXArgs.Length; i++ ) {
					var namedXArg = namedXArgs[ i ];
					var namedYArg = namedYArgs[ i ];

					if( !StringComparer.Ordinal.Equals( namedXArg.Key, namedYArg.Key ) ) {
						return false;
					}

					if( !namedXArg.Value.Equals( namedYArg.Value ) ) {
						return false;
					}
				}

				return true;
			}

			int IEqualityComparer<AttributeData>.GetHashCode( AttributeData obj ) {
				throw new NotImplementedException();
			}
		}
	}
}
