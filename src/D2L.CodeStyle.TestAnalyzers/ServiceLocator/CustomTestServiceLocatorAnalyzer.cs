using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.TestAnalyzers.ServiceLocator {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class CustomTestServiceLocatorAnalyzer : DiagnosticAnalyzer {

		private const string TestServiceLocatorFactoryType
			= "D2L.LP.Extensibility.Activation.Domain.TestServiceLocatorFactory";

		private const string WhitelistFileName = "CustomServiceLocatorWhitelist.txt";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.CustomServiceLocator );

		private readonly bool _excludeKnownProblems;

		public CustomTestServiceLocatorAnalyzer() : this( true ) { }

		public CustomTestServiceLocatorAnalyzer( bool excludeKnownProblemFixtures ) {
			_excludeKnownProblems = excludeKnownProblemFixtures;
		}

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
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

			HashSet<string> whitelistedClasses = GetWhitelist(
				context.Options.AdditionalFiles
			);

			context.RegisterSyntaxNodeAction(
				ctx => PreventCustomLocatorUsage(
					ctx,
					factoryType,
					whitelistedClasses
				),
				SyntaxKind.InvocationExpression
			);
		}

		// Prevent static usage of TestServiceLocator.Create() methods.
		private void PreventCustomLocatorUsage(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol disallowedType,
			HashSet<string> whitelistedClasses
		) {
			ExpressionSyntax root = context.Node as InvocationExpressionSyntax;
			if( root == null ) {
				return;
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo( root );

			IMethodSymbol method = symbolInfo.Symbol as IMethodSymbol;

			if( method == null ) {
				if( symbolInfo.CandidateSymbols == null ) {
					return;
				}

				if( symbolInfo.CandidateSymbols.Length != 1 ) {
					return;
				}

				// This happens on method groups, such as
				// Func<IServiceLocator> fooFunc = TestServiceLocatorFactory.Create( ... );
				method = symbolInfo.CandidateSymbols.First() as IMethodSymbol;

				if( method == null ) {
					return;
				}
			}

			// If we're a Create method on a class that isn't
			// TestServiceLocatorFactory, we're safe.
			if( !IsTestServiceLocatorFactory(
					actualType: method.ContainingType,
					disallowedType: disallowedType
				)
			) {
				return;
			}

			// If we're not a Create method, we're safe.
			if( !IsCreateMethod( method ) ) {
				return;
			}

			// Check whether any parent classes are on our whitelist. Checking
			// all of the parents lets us handle partial classes more easily.
			var parentClasses = context.Node.Ancestors().Where(
				a => a.IsKind( SyntaxKind.ClassDeclaration )
			);

			var parentSymbols = parentClasses.Select(
				c => context.SemanticModel.GetDeclaredSymbol( c )
			);

			bool isWhitelisted = parentSymbols.Any(
				s => IsClassWhitelisted( whitelistedClasses, s )
			);

			if( isWhitelisted ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.CustomServiceLocator,
					context.Node.GetLocation()
				)
			);
		}

		private static bool IsTestServiceLocatorFactory(
			INamedTypeSymbol actualType,
			INamedTypeSymbol disallowedType
		) {
			if( actualType == null ) {
				return false;
			}

			bool isLocatorFactory = actualType.Equals( disallowedType );
			return isLocatorFactory;
		}

		private static bool IsCreateMethod(
			IMethodSymbol method
		) {
			return method.Name.Equals( "Create" )
				&& method.Parameters.Length > 0;
		}

		private bool IsClassWhitelisted(
			HashSet<string> whitelistedClasses,
			ISymbol classSymbol
		) {
			bool isWhiteListed = _excludeKnownProblems
				&& whitelistedClasses.Contains( classSymbol.ToString() );

			return isWhiteListed;
		}

		private HashSet<string> GetWhitelist(
			ImmutableArray<AdditionalText> additionalFiles
		) {
			HashSet<string> whitelistedClasses = new HashSet<string>();

			AdditionalText whitelistFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == WhitelistFileName
			);

			if( whitelistFile == null ) {
				return whitelistedClasses;
			}

			SourceText whitelistText = whitelistFile.GetText();

			foreach( TextLine line in whitelistText.Lines ) {
				whitelistedClasses.Add( line.ToString() );
			}

			return whitelistedClasses;
		}

	}
}
