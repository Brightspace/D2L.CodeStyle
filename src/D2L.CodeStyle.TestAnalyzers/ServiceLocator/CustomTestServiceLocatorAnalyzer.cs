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

		private const string WhitelistFileName = "CustomTestServiceLocatorWhitelist.txt";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.CustomServiceLocator, Diagnostics.UnnecessaryWhitelistEntry );

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

			ImmutableHashSet<string> whitelistedClasses = GetWhitelist(
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

			context.RegisterSymbolAction(
				ctx => PreventUnnecessaryWhitelisting(
					ctx,
					factoryType,
					whitelistedClasses
				),
				SymbolKind.NamedType
			);
		}

		// Prevent static usage of TestServiceLocator.Create() methods.
		private void PreventCustomLocatorUsage(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol disallowedType,
			ImmutableHashSet<string> whitelistedClasses
		) {
			if( !( context.Node is InvocationExpressionSyntax invocationExpression ) ) {
				return;
			}

			if( !IsTestServiceLocatorFactoryCreate(
				context.SemanticModel,
				disallowedType,
				invocationExpression
			) ) {
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

		private void PreventUnnecessaryWhitelisting(
			SymbolAnalysisContext context,
			INamedTypeSymbol factoryType,
			ImmutableHashSet<string> whitelistedClasses
		) {
			if( !( context.Symbol is INamedTypeSymbol namedType ) ) {
				return;
			}

			if( !IsClassWhitelisted( whitelistedClasses, namedType ) ) {
				return;
			}

			Location diagnosticLocation = null;
			foreach( var syntaxRef in namedType.DeclaringSyntaxReferences ) {
				var syntax = syntaxRef.GetSyntax( context.CancellationToken );

				diagnosticLocation = diagnosticLocation ?? syntax.GetLocation();

				SemanticModel model = context.Compilation.GetSemanticModel( syntax.SyntaxTree );

				var testServiceLocatorFactoryCreates = syntax
					.DescendantNodes()
					.OfType<InvocationExpressionSyntax>()
					.Where( i => IsTestServiceLocatorFactoryCreate(
						model,
						factoryType,
						i
					) );

				if( testServiceLocatorFactoryCreates.Any() ) {
					return;
				}
			}

			if( diagnosticLocation != null ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.UnnecessaryWhitelistEntry,
						diagnosticLocation
					)
				);
			}
		}

		private static bool IsTestServiceLocatorFactoryCreate(
			SemanticModel model,
			INamedTypeSymbol factoryType,
			InvocationExpressionSyntax invocationExpression
		) {
			SymbolInfo symbolInfo = model.GetSymbolInfo( invocationExpression );

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
			ImmutableHashSet<string> whitelistedClasses,
			ISymbol classSymbol
		) {
			bool isWhiteListed = _excludeKnownProblems
				&& whitelistedClasses.Contains( classSymbol.ToString() );

			return isWhiteListed;
		}

		private ImmutableHashSet<string> GetWhitelist(
			ImmutableArray<AdditionalText> additionalFiles
		) {
			ImmutableHashSet<string>.Builder whitelistedClasses = ImmutableHashSet.CreateBuilder<string>();

			AdditionalText whitelistFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == WhitelistFileName
			);

			if( whitelistFile == null ) {
				return whitelistedClasses.ToImmutableHashSet();
			}

			SourceText whitelistText = whitelistFile.GetText();

			foreach( TextLine line in whitelistText.Lines ) {
				whitelistedClasses.Add( line.ToString().Trim() );
			}

			return whitelistedClasses.ToImmutableHashSet();
		}

	}
}
