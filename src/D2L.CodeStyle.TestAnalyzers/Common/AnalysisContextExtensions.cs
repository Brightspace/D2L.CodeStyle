using System;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.Common {
	internal static class AnalysisContextExtensions {
		
		internal static void RegisterCompilationStartActionForTestProjects(
			this AnalysisContext @this,
			Action<CompilationStartAnalysisContext> action
		) {
			@this.RegisterCompilationStartAction(
				( CompilationStartAnalysisContext context ) => {
					var references = context.Compilation.ReferencedAssemblyNames;
					if( !references.Any( r => r.Name.ToUpper().Contains( "NUNIT" ) ) ) {
						// Compilation is not a test assembly, skip
						return;
					}

					action( context );
				}
			);
		}

	}
}
