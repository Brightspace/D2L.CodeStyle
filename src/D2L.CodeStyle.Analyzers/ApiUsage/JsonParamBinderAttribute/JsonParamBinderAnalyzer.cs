using System.Collections.Immutable;
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
			Diagnostics.UnnecessaryAllowedListEntry
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			var attributeType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Web.Rest.Attributes.JsonParamBinder" );
			if( attributeType == null ) {
				// Attribute is presumably not being used, so no need to register our analyzer
				return;
			}

			AllowedTypeList allowedTypeList = AllowedTypeList.CreateFromAnalyzerOptions(
				allowedListFileName: "LegacyJsonParamBinderAllowedList.txt",
				analyzerOptions: context.Options
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeAttribute( ctx, attributeType, allowedTypeList ),
				SyntaxKind.Attribute
			);

			context.RegisterSymbolAction(
				ctx => PreventUnnecessaryAllowedListing(
					ctx,
					attributeType,
					allowedTypeList
				),
				SymbolKind.NamedType
			);
		}

		private void AnalyzeAttribute(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol jsonParamBinderT,
			AllowedTypeList allowedList
		) {
			if( !( context.Node is AttributeSyntax attribute ) ) {
				return;
			}

			if( !AttributeIsOfDisallowedType( context.SemanticModel, jsonParamBinderT, attribute ) ) {
				return;
			}

			ISymbol? methodSymbol = context.ContainingSymbol;
			if( methodSymbol == null ) {
				return;
			}

			if( methodSymbol.Kind != SymbolKind.Method ) {
				return;
			}

			if( !( methodSymbol.ContainingType is INamedTypeSymbol classSymbol ) ) {
				return;
			}

			if( allowedList.Contains( classSymbol ) ) {
				return;
			}

			Location location = attribute.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(
				Diagnostics.ObsoleteJsonParamBinder,
				location
			);

			context.ReportDiagnostic( diagnostic );
		}

		private void PreventUnnecessaryAllowedListing(
			SymbolAnalysisContext context,
			INamedTypeSymbol jsonParamBinderT,
			AllowedTypeList allowedTypeList
		) {
			if( context.Symbol is not INamedTypeSymbol namedType  ) {
				return;
			}

			if( !allowedTypeList.Contains( namedType ) ) {
				return;
			}

			foreach( var method in namedType.GetMembers().OfType<IMethodSymbol>() ) {
				foreach( var parameter in method.Parameters ) {
					foreach( var attribute in parameter.GetAttributes() ) {
						if( SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, jsonParamBinderT ) ) {
							return;
						}
					}
				}
			}

			Location? diagnosticLocation = namedType.Locations.FirstOrDefault();
			if( diagnosticLocation != null ) {
				allowedTypeList.ReportEntryAsUnnecesary(
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

			if( !actualType.Equals( jsonParamBinderT, SymbolEqualityComparer.Default ) ) {
				return false;
			}

			return true;
		}

	}

}
