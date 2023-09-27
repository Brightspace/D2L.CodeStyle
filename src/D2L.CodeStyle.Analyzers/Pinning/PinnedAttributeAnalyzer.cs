using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Pinning {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class PinnedAttributeAnalyzer : DiagnosticAnalyzer {

		private static readonly TypeKind[] TypeKindsSupported = new[] { TypeKind.Class, TypeKind.Interface };
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.PinnedTypesMustNotMove
			);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSymbolAction( AnalyzeSymbol,
				SymbolKind.NamedType );
		}

		private static void AnalyzeSymbol( SymbolAnalysisContext context ) {
			INamedTypeSymbol? pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol == null ) {
				return;
			}

			INamedTypeSymbol? classSymbol = context.Symbol as INamedTypeSymbol;
			if( classSymbol == null ) {
				return;
			}

			if( !TypeKindsSupported.Contains( classSymbol.TypeKind ) ) {
				return;
			}

			Location? location = classSymbol.Locations.FirstOrDefault();
			if( !PinnedAnalyzerHelper.TryGetPinnedAttribute( classSymbol, pinnedAttributeSymbol, out AttributeData? attribute ) ) {
				return;
			}

			string? fqName = attribute?.ConstructorArguments[0].Value?.ToString();
			string? assembly = attribute?.ConstructorArguments[1].Value?.ToString();
			if( fqName == null || assembly == null ) {
				context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinnedTypesMustNotMove, location, classSymbol.Name ) );
				return;
			}

			SymbolDisplayFormat fullyQualifiedNameFormat = new SymbolDisplayFormat(
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
			);

			string classFqName = classSymbol.ToDisplayString( fullyQualifiedNameFormat );

			if( fqName != classFqName
			    || assembly != classSymbol.ContainingAssembly.Name
			  ) {
				context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinnedTypesMustNotMove, location, classSymbol.Name ) );
			}
		}
	}
}
