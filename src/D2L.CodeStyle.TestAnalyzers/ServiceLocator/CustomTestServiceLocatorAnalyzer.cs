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

		private const string AllowedListFileName = "CustomTestServiceLocatorAllowedList.txt";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.CustomServiceLocator, Diagnostics.UnnecessaryAllowedListEntry );

		private readonly bool _excludeKnownProblems;

		public CustomTestServiceLocatorAnalyzer() : this( true ) { }

		public CustomTestServiceLocatorAnalyzer( bool excludeKnownProblemFixtures ) {
			_excludeKnownProblems = excludeKnownProblemFixtures;
		}

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

			ImmutableHashSet<string> allowedClasses = GetAllowedList(
				context.Options.AdditionalFiles
			);

			context.RegisterSyntaxNodeAction(
				ctx => PreventCustomLocatorUsage(
					ctx,
					factoryType,
					allowedClasses
				),
				SyntaxKind.InvocationExpression
			);

			context.RegisterSymbolAction(
				ctx => PreventUnnecessaryAllowedListing(
					ctx,
					factoryType,
					allowedClasses
				),
				SymbolKind.NamedType
			);
		}

		// Prevent static usage of TestServiceLocator.Create() methods.
		private void PreventCustomLocatorUsage(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol disallowedType,
			ImmutableHashSet<string> allowedClasses
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

			// Check whether any parent classes are on our allowed list. Checking
			// all of the parents lets us handle partial classes more easily.
			var parentClasses = context.Node.Ancestors().Where(
				a => a.IsKind( SyntaxKind.ClassDeclaration )
			);

			var parentSymbols = parentClasses.Select(
				c => context.SemanticModel.GetDeclaredSymbol( c )
			);

			bool isAllowed = parentSymbols.Any(
				s => IsClassAllowed( allowedClasses, s )
			);

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

		private void PreventUnnecessaryAllowedListing(
			SymbolAnalysisContext context,
			INamedTypeSymbol factoryType,
			ImmutableHashSet<string> allowedClasses
		) {
			if( !( context.Symbol is INamedTypeSymbol namedType ) ) {
				return;
			}

			if( !IsClassAllowed( allowedClasses, namedType ) ) {
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
						Diagnostics.UnnecessaryAllowedListEntry,
						diagnosticLocation,
						GetAllowedListName( namedType ), AllowedListFileName
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

			bool isLocatorFactory = actualType.Equals( disallowedType, SymbolEqualityComparer.Default );
			return isLocatorFactory;
		}

		private static bool IsCreateMethod(
			IMethodSymbol method
		) {
			return method.Name.Equals( "Create" )
				&& method.Parameters.Length > 0;
		}

		private bool IsClassAllowed(
			ImmutableHashSet<string> allowedClasses,
			ISymbol classSymbol
		) {
			bool isAllowed = _excludeKnownProblems
				&& allowedClasses.Contains( GetAllowedListName( classSymbol ) );

			return isAllowed;
		}

		private static string GetAllowedListName( ISymbol classSymbol ) =>
			classSymbol.ToString()
			+ ", "
			+ classSymbol.ContainingAssembly.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat )
		;

		private static ImmutableHashSet<string> GetAllowedList(
			ImmutableArray<AdditionalText> additionalFiles
		) {
			ImmutableHashSet<string>.Builder allowedClasses = ImmutableHashSet.CreateBuilder<string>();

			AdditionalText allowedListFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == AllowedListFileName
			);

			if( allowedListFile == null ) {
				return allowedClasses.ToImmutableHashSet();
			}

			SourceText allowedListText = allowedListFile.GetText();

			foreach( TextLine line in allowedListText.Lines ) {
				allowedClasses.Add( line.ToString().Trim() );
			}

			return allowedClasses.ToImmutableHashSet();
		}

	}
}
