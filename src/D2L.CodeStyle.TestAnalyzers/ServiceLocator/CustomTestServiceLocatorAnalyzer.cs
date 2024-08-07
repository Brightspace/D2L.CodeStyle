using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Extensions;
using D2L.CodeStyle.TestAnalyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.TestAnalyzers.ServiceLocator {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class CustomTestServiceLocatorAnalyzer : DiagnosticAnalyzer {

		private const string TestServiceLocatorFactoryType
			= "D2L.LP.Extensibility.Activation.Domain.TestServiceLocatorFactory";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.CustomServiceLocator, Diagnostics.UnnecessaryAllowedListEntry );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction(
				RegisterServiceLocatorAnalyzer
			);
		}

		public static void RegisterServiceLocatorAnalyzer(
			CompilationStartAnalysisContext context
		) {
			INamedTypeSymbol? factoryType = context.Compilation
				.GetTypeByMetadataName( TestServiceLocatorFactoryType );

			if( factoryType.IsNullOrErrorType() ) {
				return;
			}

			ImmutableHashSet<IMethodSymbol> createMethods = factoryType
				.GetMembers( "Create" )
				.OfType<IMethodSymbol>()
				.ToImmutableHashSet<IMethodSymbol>( SymbolEqualityComparer.Default );

			if( createMethods.IsEmpty ) {
				return;
			}

			AllowedTypeList allowedTypeList = AllowedTypeList.CreateFromAnalyzerOptions(
				allowedListFileName: "CustomTestServiceLocatorAllowedList.txt",
				analyzerOptions: context.Options
			);

			context.RegisterOperationAction(
				ctx => PreventCustomLocatorUsage(
					ctx,
					( (IInvocationOperation)ctx.Operation ).TargetMethod,
					allowedTypeList,
					createMethods
				),
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				ctx => PreventCustomLocatorUsage(
					ctx,
					( (IMethodReferenceOperation)ctx.Operation ).Method,
					allowedTypeList,
					createMethods
				),
				OperationKind.MethodReference
			);

			context.RegisterSymbolAction(
				allowedTypeList.CollectSymbolIfContained,
				SymbolKind.NamedType
			);

			context.RegisterCompilationEndAction(
				allowedTypeList.ReportUnnecessaryEntries
			);
		}

		// Prevent static usage of TestServiceLocator.Create() methods.
		private static void PreventCustomLocatorUsage(
			OperationAnalysisContext context,
			IMethodSymbol methodSymbol,
			AllowedTypeList allowedTypeList,
			ImmutableHashSet<IMethodSymbol> createMethods
		) {
			if( !createMethods.Contains( methodSymbol ) ) {
				return;
			}

			ImmutableArray<INamedTypeSymbol> containingTypes = context.ContainingSymbol.GetAllContainingTypes();

			if( containingTypes.Any( allowedTypeList.Contains ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.CustomServiceLocator,
					context.Operation.Syntax.GetLocation()
				)
			);
		}

	}
}
