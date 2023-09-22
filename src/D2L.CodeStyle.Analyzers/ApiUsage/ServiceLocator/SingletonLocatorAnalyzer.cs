using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class SingletonLocatorAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.SingletonLocatorMisuse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			IMethodSymbol? getMethodSymbol = GetSingletonLocatorGetMethod( context.Compilation );
			if( getMethodSymbol == null ) {
				return;
			}

			ContainedTypeResolver containedTypeResolver = GetContainedTypeResolver( context.Compilation );

			context.RegisterOperationAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					( (IInvocationOperation)ctx.Operation ).TargetMethod,
					IsSingletonLocatorGet,
					containedTypeResolver
				),
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					( (IMethodReferenceOperation)ctx.Operation ).Method,
					IsSingletonLocatorGet,
					containedTypeResolver
				),
				OperationKind.MethodReference
			);

			bool IsSingletonLocatorGet( IMethodSymbol? methodSymbol ) => SymbolEqualityComparer.Default.Equals(
				getMethodSymbol,
				methodSymbol?.OriginalDefinition
			);
		}

		private delegate bool IsSingletonLocatorGet(
			IMethodSymbol? methodSymbol
		);

		// Enforce that SingletonLocator can only load actual [Singleton]s
		private static void EnforceSingletonsOnly(
			OperationAnalysisContext context,
			IMethodSymbol methodSymbol,
			IsSingletonLocatorGet isSingletonLocatorGet,
			ContainedTypeResolver containedTypeResolver
		) {
			if( !isSingletonLocatorGet( methodSymbol ) ) {
				return;
			}

			ITypeSymbol typeArg = containedTypeResolver(
				methodSymbol.TypeArguments.Single()
			);

			if( Attributes.Singleton.IsDefined( typeArg ) ) {
				return;
			}

			context.ReportDiagnostic(
				descriptor: Diagnostics.SingletonLocatorMisuse,
				location: context.Operation.Syntax.GetLocation(),
				messageArgs: new[] { typeArg.GetFullTypeName() }
			);
		}

		private static IMethodSymbol? GetSingletonLocatorGetMethod(
			Compilation compilation
		) {
			INamedTypeSymbol? locatorType = compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.SingletonLocator" );

			if( locatorType.IsNullOrErrorType() ) {
				return null;
			}

			ImmutableArray<ISymbol> getMembers = locatorType.GetMembers( "Get" );
			if( getMembers.Length != 1 ) {
				throw new InvalidOperationException( "Unexpected result when locating SingletonLocator.Get<> method." );
			}

			return (IMethodSymbol)getMembers[ 0 ];
		}

		internal delegate ITypeSymbol ContainedTypeResolver( ITypeSymbol type );
		internal static ContainedTypeResolver GetContainedTypeResolver( Compilation compilation ) {
			ImmutableDictionary<INamedTypeSymbol, int> containerTypes = GetContainerTypes( compilation );

			return type => {
				if( type is not INamedTypeSymbol namedType ) {
					return type;
				}

				if( containerTypes.TryGetValue( namedType.OriginalDefinition, out int typeArgumentIndex ) ) {
					return namedType.TypeArguments[ typeArgumentIndex ];
				}

				return type;
			};
		}

		private static ImmutableDictionary<INamedTypeSymbol, int> GetContainerTypes( Compilation compilation ) {
			var containerTypeMappings = new (string typeName, int containedTypeIdx)[] {
				new ( "D2L.LP.Extensibility.Activation.Domain.IPlugins`1", 0 ),
				new ( "D2L.LP.Extensibility.Activation.Domain.IPlugins`2", 1 ),
			};

			var containerTypesBuilder = ImmutableDictionary.CreateBuilder<INamedTypeSymbol, int>( SymbolEqualityComparer.Default );

			foreach( (string typeName, int containedTypeIdx) in containerTypeMappings ) {

				INamedTypeSymbol? type = compilation.GetTypeByMetadataName( typeName );
				if( !type.IsNullOrErrorType() ) {

					containerTypesBuilder.Add( type, containedTypeIdx );
				}
			}

			return containerTypesBuilder.ToImmutable();
		}

	}
}
