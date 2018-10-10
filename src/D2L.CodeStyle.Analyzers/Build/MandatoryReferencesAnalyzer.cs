using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Build {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class MandatoryReferencesAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create( Diagnostics.MustReferenceAnnotations );

		public override void Initialize( AnalysisContext ctx ) {
			ctx.EnableConcurrentExecution();
			ctx.RegisterCompilationAction( AnalyzeCompilation );
		}

		public static void AnalyzeCompilation(
			CompilationAnalysisContext ctx
		) {
			bool hasAnnotationsReference = ctx.Compilation
				.References
				.Any( IsTheAnnotationsAssembly );

			if ( !hasAnnotationsReference ) {
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.MustReferenceAnnotations,
						Location.None
					)
				);
			}
		}

		public static bool IsTheAnnotationsAssembly( MetadataReference mr ) {
			return mr.Display.EndsWith( "\\D2L.CodeStyle.Annotations.dll" );
		}
	}
}
