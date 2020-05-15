using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class JsonParamBinderAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ObsoleteJsonParamBinder,
			Diagnostics.UnnecessaryWhitelistEntry
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			var attributeType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Web.Rest.Attributes.JsonParamBinder" );
			if( attributeType == null ) {
				// Attribute is presumably not being used, so no need to register our analyzer
				return;
			}

			TypeWhitelist typeWhitelist = TypeWhitelist.CreateFromAnalyzerOptions(
				whitelistFileName: "LegacyJsonParamBinderWhitelist.txt",
				analyzerOptions: context.Options
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeAttribute( ctx, attributeType, typeWhitelist ),
				SyntaxKind.Attribute
			);

			context.RegisterSymbolAction(
				ctx => PreventUnnecessaryWhitelisting(
					ctx,
					attributeType,
					typeWhitelist
				),
				SymbolKind.NamedType
			);
		}

		private void AnalyzeAttribute(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol jsonParamBinderT,
			TypeWhitelist whitelist
		) {
			if( !( context.Node is AttributeSyntax attribute ) ) {
				return;
			}

			if( !AttributeIsOfDisallowedType( context.SemanticModel, jsonParamBinderT, attribute ) ) {
				return;
			}

			ISymbol methodSymbol = context.ContainingSymbol;
			if( methodSymbol.Kind != SymbolKind.Method ) {
				return;
			}

			if( !( methodSymbol.ContainingType is INamedTypeSymbol classSymbol ) ) {
				return;
			}

			if( whitelist.Contains( classSymbol ) ) {
				return;
			}

			Location location = attribute.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(
				Diagnostics.ObsoleteJsonParamBinder,
				location
			);

			context.ReportDiagnostic( diagnostic );
		}

		private void PreventUnnecessaryWhitelisting(
			SymbolAnalysisContext context,
			INamedTypeSymbol jsonParamBinderT,
			TypeWhitelist typeWhitelist
		) {
			if( !( context.Symbol is INamedTypeSymbol namedType ) ) {
				return;
			}

			if( !typeWhitelist.Contains( namedType ) ) {
				return;
			}

			Location diagnosticLocation = null;
			foreach( var syntaxRef in namedType.DeclaringSyntaxReferences ) {
				var typeSyntax = syntaxRef.GetSyntax( context.CancellationToken ) as TypeDeclarationSyntax;

				diagnosticLocation = diagnosticLocation ?? typeSyntax.Identifier.GetLocation();

				SemanticModel model = context.Compilation.GetSemanticModel( typeSyntax.SyntaxTree );

				bool usesDisallowedTypes = typeSyntax
					.DescendantNodes()
					.OfType<AttributeSyntax>()
					.Any( syntax => AttributeIsOfDisallowedType( model, jsonParamBinderT, syntax ) );

				if( usesDisallowedTypes ) {
					return;
				}
			}

			if( diagnosticLocation != null ) {
				typeWhitelist.ReportEntryAsUnnecesary(
					entry: namedType,
					location: diagnosticLocation,
					report: context.ReportDiagnostic
				);
			}
		}

		private static bool AttributeIsOfDisallowedType(
			SemanticModel model,
			INamedTypeSymbol jsonParamBinderT,
			AttributeSyntax syntax
		) {
			if( !( model.GetTypeInfo( syntax ).Type is INamedTypeSymbol actualType ) ) {
				return false;
			}

			if( !actualType.Equals( jsonParamBinderT ) ) {
				return false;
			}

			return true;
		}

	}

}
