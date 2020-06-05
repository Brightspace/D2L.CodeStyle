using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class OldAndBrokenServiceLocatorAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.OldAndBrokenLocatorIsObsolete,
			Diagnostics.UnnecessaryAllowedListEntry
		);
		private readonly bool _excludeKnownProblems;

		public OldAndBrokenServiceLocatorAnalyzer() : this( true ) { }

		public OldAndBrokenServiceLocatorAnalyzer(
			bool excludeKnownProblemDlls
		) {
			_excludeKnownProblems = excludeKnownProblemDlls;
		}

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterServiceLocatorAnalyzer );
		}

		public void RegisterServiceLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocator" );
			var factoryType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocatorFactory" );
			var activatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.IObjectActivator" );
			var customActivatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.ICustomObjectActivator" );

			// If those type lookups failed then OldAndBrokenServiceLocator
			// cannot resolve and we don't need to register our analyzer.

			if( locatorType == null || locatorType.Kind == SymbolKind.ErrorType ) {
				return;
			}
			if ( factoryType == null || factoryType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			ImmutableArray<INamedTypeSymbol> disallowedTypes = ImmutableArray.Create(
				locatorType,
				factoryType,
				activatorType,
				customActivatorType
			);

			TypeAllowedList typeAllowedList = TypeAllowedList.CreateFromAnalyzerOptions(
				allowedListFileName: "OldAndBrokenServiceLocatorAllowedList.txt",
				analyzerOptions: context.Options
			);

			//Prevent static usage of OldAndBrokenServiceLocator
			//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
			context.RegisterSyntaxNodeAction(
				ctx => PreventOldAndBrokenUsage(
					ctx,
					disallowedTypes,
					typeAllowedList
				),
				SyntaxKind.IdentifierName
			);

			context.RegisterSymbolAction(
				ctx => PreventUnnecessaryAllowedListing(
					ctx,
					disallowedTypes,
					typeAllowedList
				),
				SymbolKind.NamedType
			);
		}

		//Prevent static usage of OldAndBrokenServiceLocator
		//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
		private void PreventOldAndBrokenUsage(
			SyntaxNodeAnalysisContext context,
			ImmutableArray<INamedTypeSymbol> disallowededTypes,
			TypeAllowedList typeAllowedList
		) {
			if( !( context.Node is IdentifierNameSyntax syntax ) ) {
				return;
			}

			if( !IdentifierIsOfDisallowededType( context.SemanticModel, disallowededTypes, syntax ) ) {
				return;
			}

			var parentClasses = context.Node.Ancestors().OfType<TypeDeclarationSyntax>();
			var parentSymbols = parentClasses.Select( c => context.SemanticModel.GetDeclaredSymbol( c ) ).ToImmutableArray();

			if( parentSymbols.Any( s => Attributes.DIFramework.IsDefined( s ) ) ) {
				//Classes in the DI Framework are alloweded to use locators and activators
				return;
			}

			if( _excludeKnownProblems && parentSymbols.Any( typeAllowedList.Contains ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, syntax.GetLocation() )
			);
		}

		private void PreventUnnecessaryAllowedListing(
			SymbolAnalysisContext context,
			ImmutableArray<INamedTypeSymbol> disallowededTypes,
			TypeAllowedList typeAllowedList
		) {
			if( !( context.Symbol is INamedTypeSymbol namedType ) ) {
				return;
			}

			if( !typeAllowedList.Contains( namedType ) ) {
				return;
			}

			Location diagnosticLocation = null;
			foreach( var syntaxRef in namedType.DeclaringSyntaxReferences ) {
				var typeSyntax = syntaxRef.GetSyntax( context.CancellationToken ) as TypeDeclarationSyntax;

				diagnosticLocation = diagnosticLocation ?? typeSyntax.Identifier.GetLocation();

				SemanticModel model = context.Compilation.GetSemanticModel( typeSyntax.SyntaxTree );

				bool usesDisallowededTypes = typeSyntax
					.DescendantNodes()
					.OfType<IdentifierNameSyntax>()
					.Any( syntax => IdentifierIsOfDisallowededType( model, disallowededTypes, syntax ) );

				if( usesDisallowededTypes ) {
					return;
				}
			}

			if( diagnosticLocation != null ) {
				typeAllowedList.ReportEntryAsUnnecesary(
					entry: namedType,
					location: diagnosticLocation,
					report: context.ReportDiagnostic
				);
			}
		}

		private static bool IdentifierIsOfDisallowededType(
			SemanticModel model,
			ImmutableArray<INamedTypeSymbol> disallowedTypes,
			IdentifierNameSyntax syntax
		) {
			var actualType = model.GetTypeInfo( syntax ).Type as INamedTypeSymbol;

			if( actualType == null ) {
				return false;
			}

			if( !disallowedTypes.Contains( actualType ) ) {
				return false;
			}

			return true;
		}
	}
}
