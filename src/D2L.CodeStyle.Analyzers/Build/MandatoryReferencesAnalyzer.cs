#nullable disable

using System.Collections.Immutable;
using System.IO;
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
			ctx.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			ctx.RegisterCompilationAction( AnalyzeCompilation );
		}

		public static void AnalyzeCompilation(
			CompilationAnalysisContext ctx
		) {
			bool hasAnnotationsReference = HasAnnotationsReference( ctx.Compilation );

			if ( !hasAnnotationsReference ) {
				ctx.ReportDiagnostic(
					Diagnostics.MustReferenceAnnotations,
					Location.None
				);
			}

			// TODO:
			// * How does VS sometimes offer you "Add reference to foo.dll" as
			//   a fix?
			// * Does it look at what other projects in the solution reference
			//   to get a symbol? That'd be  too complicated to implement here.
			// * Can we use their fix somehow?
			// * Should we open up a GitHub issue for this?
		}

		public static bool HasAnnotationsReference( Compilation compilation ) {
			return compilation
				.References
				.Any( IsTheAnnotationsAssembly );
		}

		private static bool IsTheAnnotationsAssembly( MetadataReference mr ) {
			// There are 3 types of MetadataReference in Roslyn currently:
			// * UnresolvedMetadataReference: this will have a Dispaly of
			//   "Unresolved: <name>"
			// * PortableExecutableReference: it will be a path
			// * CompilationReference: it will be the name of the assembly.
			//   This gets used when you use Roslyn "manually", I'm not sure it
			//   comes up in practice...
			// So we're going to assume/require that Annotations is referenced
			// by a path on disk to a DLL.
			var pathOrName = mr.Display;

			string path;

			try {
				path = Path.GetFullPath( pathOrName );
			} catch { // all exceptions
				// if we can't parse the path for some reason, assume this
				// isn't the annotations assembly to be safe.
				return false;
			}

			string filename = Path.GetFileName( path );

			// We're only checking that you reference something with this
			// file name... maybe we could do something better with strong
			// naming? That might make rollying out changes to the
			// annotations a bit more annoying though.
			return filename == "D2L.CodeStyle.Annotations.dll";
		}
	}
}
