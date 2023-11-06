using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Pinning {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class PinnedAttributeAnalyzer : DiagnosticAnalyzer {

		private static SymbolDisplayFormat FullyQualifiedNameFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
		);

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
			INamedTypeSymbol? pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol != null ) {
				context.RegisterSymbolAction( ( ctx ) => AnalyzeSymbol(ctx, pinnedAttributeSymbol),
					SymbolKind.NamedType );
			}
		}

		private static void AnalyzeSymbol( SymbolAnalysisContext context, INamedTypeSymbol pinnedAttributeSymbol ) {
			INamedTypeSymbol classSymbol = (INamedTypeSymbol)context.Symbol;

			Location? location = classSymbol.Locations.FirstOrDefault();
			var attribute = PinnedAnalyzerHelper.GetPinnedAttribute( classSymbol, pinnedAttributeSymbol );
			if( attribute == null ) {
				return;
			}

			string? fqName = attribute.ConstructorArguments[0].Value?.ToString();
			string? assembly = attribute.ConstructorArguments[1].Value?.ToString();
			if( fqName == null || assembly == null ) {
				context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinnedTypesMustNotMove, location, classSymbol.Name ) );
				return;
			}

			string classFqName = classSymbol.ToDisplayString( FullyQualifiedNameFormat );

			if( fqName != classFqName
			    || assembly != classSymbol.ContainingAssembly.Name
			  ) {
				context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinnedTypesMustNotMove, location, classSymbol.Name ) );
			}
		}
	}
}
