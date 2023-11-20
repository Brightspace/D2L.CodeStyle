using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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

			context.RegisterOperationAction(
				ctx => AnalyzeAttribute( ctx, attributeType, allowedTypeList ),
				OperationKind.Attribute
			);

			context.RegisterSymbolAction(
				allowedTypeList.CollectSymbolIfContained,
				SymbolKind.NamedType
			);

			context.RegisterCompilationEndAction(
				allowedTypeList.ReportUnnecessaryEntries
			);
		}

		private static void AnalyzeAttribute(
			OperationAnalysisContext context,
			INamedTypeSymbol jsonParamBinderT,
			AllowedTypeList allowedList
		) {
			IAttributeOperation operation = ( IAttributeOperation )context.Operation;

			if( operation.Operation is not IObjectCreationOperation attributeCreationOperation ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( jsonParamBinderT, attributeCreationOperation.Type ) ) {
				return;
			}

			if( context.ContainingSymbol.ContainingType is not INamedTypeSymbol containingType ) {
				return;
			}

			if( allowedList.Contains( containingType ) ) {
				return;
			}

			Location location = operation.Syntax.GetLocation();

			context.ReportDiagnostic(
				Diagnostics.ObsoleteJsonParamBinder,
				location
			);
		}

	}

}
