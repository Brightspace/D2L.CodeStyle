using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

		public void RegisterServiceLocatorAnalyzer(
			CompilationStartAnalysisContext context
		) {
			INamedTypeSymbol factoryType = context.Compilation
				.GetTypeByMetadataName( TestServiceLocatorFactoryType );

			if( factoryType == null || factoryType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			AllowedTypeList allowedTypeList = AllowedTypeList.CreateFromAnalyzerOptions(
				allowedListFileName: "CustomTestServiceLocatorAllowedList.txt",
				analyzerOptions: context.Options
			);

			context.RegisterSyntaxNodeAction(
				ctx => PreventCustomLocatorUsage(
					ctx,
					factoryType,
					allowedTypeList
				),
				SyntaxKind.InvocationExpression
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
		private void PreventCustomLocatorUsage(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol disallowedType,
			AllowedTypeList allowedTypeList
		) {
			if( !( context.Node is InvocationExpressionSyntax invocationExpression ) ) {
				return;
			}

			if( !IsTestServiceLocatorFactoryCreate(
				context.SemanticModel,
				disallowedType,
				invocationExpression,
				context.CancellationToken
			) ) {
				return;
			}

			// Check whether any parent classes are on our allowed list. Checking
			// all of the parents lets us handle partial classes more easily.
			var parentClasses = context.Node.Ancestors().OfType<ClassDeclarationSyntax>();

			var parentSymbols = parentClasses.Select(
				c => context.SemanticModel.GetDeclaredSymbol( c, context.CancellationToken )
			);

			bool isAllowed = parentSymbols.Any( allowedTypeList.Contains );

			if( isAllowed ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.CustomServiceLocator,
					context.Node.GetLocation()
				)
			);
		}

		private static bool IsTestServiceLocatorFactoryCreate(
			SemanticModel model,
			INamedTypeSymbol factoryType,
			InvocationExpressionSyntax invocationExpression,
			CancellationToken cancellationToken
		) {
			SymbolInfo symbolInfo = model.GetSymbolInfo( invocationExpression, cancellationToken );

			IMethodSymbol method = symbolInfo.Symbol as IMethodSymbol;

			if( method == null ) {
				if( symbolInfo.CandidateSymbols == null ) {
					return false;
				}

				if( symbolInfo.CandidateSymbols.Length != 1 ) {
					return false;
				}

				// This happens on method groups, such as
				// Func<IServiceLocator> fooFunc = TestServiceLocatorFactory.Create( ... );
				method = symbolInfo.CandidateSymbols.First() as IMethodSymbol;

				if( method == null ) {
					return false;
				}
			}

			// If we're a Create method on a class that isn't
			// TestServiceLocatorFactory, we're safe.
			if( !IsTestServiceLocatorFactory(
					actualType: method.ContainingType,
					disallowedType: factoryType
				)
			) {
				return false;
			}

			// If we're not a Create method, we're safe.
			if( !IsCreateMethod( method ) ) {
				return false;
			}

			return true;
		}

		private static bool IsTestServiceLocatorFactory(
			INamedTypeSymbol actualType,
			INamedTypeSymbol disallowedType
		) {
			if( actualType == null ) {
				return false;
			}

			bool isLocatorFactory = actualType.Equals( disallowedType, SymbolEqualityComparer.Default );
			return isLocatorFactory;
		}

		private static bool IsCreateMethod(
			IMethodSymbol method
		) {
			return method.Name.Equals( "Create" )
				&& method.Parameters.Length > 0;
		}

	}
}
