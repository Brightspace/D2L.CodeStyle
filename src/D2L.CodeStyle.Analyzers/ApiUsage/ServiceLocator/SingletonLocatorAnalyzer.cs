using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

			ImmutableDictionary<INamedTypeSymbol, int> containerTypes = GetContainerTypes( context.Compilation );

			context.RegisterOperationAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					( (IInvocationOperation)ctx.Operation ).TargetMethod,
					IsSingletonLocatorGet,
					IsContainerType
				),
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					( (IMethodReferenceOperation)ctx.Operation ).Method,
					IsSingletonLocatorGet,
					IsContainerType
				),
				OperationKind.MethodReference
			);

			bool IsSingletonLocatorGet( IMethodSymbol? methodSymbol ) => SymbolEqualityComparer.Default.Equals(
				getMethodSymbol,
				methodSymbol?.OriginalDefinition
			);

			bool IsContainerType(
					ITypeSymbol type,
					[NotNullWhen( true )] out ITypeSymbol? containedType
				) {

				if( type is not INamedTypeSymbol namedType  ) {
					containedType = null;
					return false;
				}

				if( containerTypes.TryGetValue( namedType.OriginalDefinition, out int typeArgumentIndex ) ) {
					containedType = namedType.TypeArguments[ typeArgumentIndex ];
					return true;
				}

				containedType = null;
				return false;
			}
		}

		private delegate bool IsSingletonLocatorGet(
			IMethodSymbol? methodSymbol
		);

		private delegate bool IsContainerType(
			ITypeSymbol type,
			[NotNullWhen( true )] out ITypeSymbol? containedType
		);

		// Enforce that SingletonLocator can only load actual [Singleton]s
		private static void EnforceSingletonsOnly(
			OperationAnalysisContext context,
			IMethodSymbol methodSymbol,
			IsSingletonLocatorGet isSingletonLocatorGet,
			IsContainerType isContainerType
		) {
			if( !isSingletonLocatorGet( methodSymbol ) ) {
				return;
			}

			ITypeSymbol typeArg = methodSymbol.TypeArguments.Single();
			if( isContainerType( typeArg, out ITypeSymbol? containedType ) ) {
				typeArg = containedType;
			}

			if( Attributes.Singleton.IsDefined( typeArg ) ) {
				return;
			}

			context.ReportDiagnostic( Diagnostic.Create(
				descriptor: Diagnostics.SingletonLocatorMisuse,
				location: context.Operation.Syntax.GetLocation(),
				typeArg.GetFullTypeName()
			) );
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

		private static ImmutableDictionary<INamedTypeSymbol, int> GetContainerTypes( Compilation compilation ) {
			var containerTypeMappings = new (string typeName, int containedTypeIdx)[] {
				new ( "D2L.LP.Extensibility.Activation.Domain.IPlugins`1", 0 ),
				new ( "D2L.LP.Extensibility.Activation.Domain.IPlugins`2", 1 ),
				new ( "D2L.LP.Extensibility.Plugins.IInstancePlugins`1",   0 ),
				new ( "D2L.LP.Extensibility.Plugins.IInstancePlugins`2",   0 )
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
