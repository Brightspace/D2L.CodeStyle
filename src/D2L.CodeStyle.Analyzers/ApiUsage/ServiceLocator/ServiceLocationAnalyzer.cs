#nullable enable

using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator.SingletonLocatorAnalyzer;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ServiceLocationAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.LocatedUnlocatable
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			LocatedTypeResolver? locatedTypeResolver = GetLocatedTypeResolver( context.Compilation );
			if( locatedTypeResolver is null ) {
				return;
			}

			context.RegisterOperationAction(
				ctx => EnforceLocatorRules(
					ctx,
					( (IInvocationOperation)ctx.Operation ).TargetMethod,
					locatedTypeResolver
				),
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				ctx => EnforceLocatorRules(
					ctx,
					( (IMethodReferenceOperation)ctx.Operation ).Method,
					locatedTypeResolver
				),
				OperationKind.MethodReference
			);
		}

		private static void EnforceLocatorRules(
			OperationAnalysisContext context,
			IMethodSymbol methodSymbol,
			LocatedTypeResolver locatedTypeResolver
		) {
			ITypeSymbol? locatedType = locatedTypeResolver( methodSymbol );
			if( locatedType is null ) {
				return;
			}

			EnforceUnlocatable( context, locatedType );
		}

		// Enforce that the locator isn't used to load [Unlocatable] or [Unlocatable.Candidate]
		private static void EnforceUnlocatable(
			OperationAnalysisContext context,
			ITypeSymbol locatedType
		) {
			if( !Attributes.Unlocatable.IsDefined( locatedType )
				&& !Attributes.UnlocatableCandidate.IsDefined( locatedType )
			) {
				return;
			}

			context.ReportDiagnostic(
				descriptor: Diagnostics.LocatedUnlocatable,
				location: context.Operation.Syntax.GetLocation(),
				messageArgs: new[] { locatedType.GetFullTypeName() }
			);
		}

		private delegate ITypeSymbol? LocatedTypeResolver( IMethodSymbol method );

		private static LocatedTypeResolver? GetLocatedTypeResolver( Compilation compilation ) {
			ImmutableDictionary<IMethodSymbol, Func<IMethodSymbol, ITypeSymbol>> locatorMethods = GetLocatorMethods( compilation );

			if( locatorMethods.IsEmpty ) {
				return null;
			}

			ContainedTypeResolver containedTypeResolver = GetContainedTypeResolver( compilation );

			return method => {
				if( !locatorMethods.TryGetValue( method.OriginalDefinition, out var resolver ) ) {
					return null;
				}

				ITypeSymbol type = resolver( method );
				type = containedTypeResolver( type );
				return type;
			};
		}

		private static ImmutableDictionary<IMethodSymbol, Func<IMethodSymbol, ITypeSymbol>> GetLocatorMethods( Compilation compilation ) {
			var locatorMethodMappings = new (string typeName, string methodName, Func<IMethodSymbol, bool> methodPicker, Func<IMethodSymbol, ITypeSymbol> typeResolver)[] {
				new( "D2L.LP.Extensibility.Activation.Domain.SingletonLocator", "Get", _ => true, m => m.TypeArguments.Single() ),
				new( "D2L.LP.Extensibility.Activation.Domain.IServiceLocator", "Get", m => m.TypeParameters.Length == 1, m => m.TypeArguments.Single() ),
				new( "D2L.LP.Extensibility.Activation.Domain.IServiceLocator", "TryGet", m => m.TypeParameters.Length == 1, m => m.TypeArguments.Single() ),
			};

			var locatorMethodsBuilder = ImmutableDictionary.CreateBuilder<IMethodSymbol, Func<IMethodSymbol, ITypeSymbol>>( SymbolEqualityComparer.Default );

			foreach( (string typeName, string methodName, var methodPicker, var typeResolver ) in locatorMethodMappings ) {

				INamedTypeSymbol? type = compilation.GetTypeByMetadataName( typeName );
				if( type.IsNullOrErrorType() ) {
					continue;
				}

				IMethodSymbol? method = type
					.GetMembers( methodName )
					.OfType<IMethodSymbol>()
					.Single( methodPicker );

				locatorMethodsBuilder.Add( method, typeResolver );
			}

			return locatorMethodsBuilder.ToImmutable();
		}

	}
}
