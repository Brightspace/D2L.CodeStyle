using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public static class AnalyzerDiagnosticsProvider {

		public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
				DiagnosticAnalyzer analyzer,
				ImmutableArray<AdditionalText> additionalFiles,
				string debugName,
				ImmutableArray<MetadataReference> metadataReferences,
				string source
			) {

			Project project = CreateSpecTestProject( debugName, metadataReferences, source );

			Compilation? compilation = await project.GetCompilationAsync();
			if( compilation == null ) {
				throw new InvalidOperationException( "Failed to compile spec test project" );
			}

			ImmutableArray<Diagnostic> diagnostics = await compilation
				.WithAnalyzers(
					analyzers: ImmutableArray.Create( analyzer ),
					options: new AnalyzerOptions( additionalFiles )
				)
				.GetAnalyzerDiagnosticsAsync();

			return diagnostics;
		}

		private static Project CreateSpecTestProject(
				string debugName,
				ImmutableArray<MetadataReference> metadataReferences,
				string source
			) {

			ProjectId projectId = ProjectId.CreateNewId( debugName );
			string filename = debugName + ".cs";
			DocumentId documentId = DocumentId.CreateNewId( projectId, debugName: debugName );

			Solution solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, debugName, debugName, LanguageNames.CSharp )
				.AddMetadataReferences( projectId, metadataReferences )
				.AddDocument( documentId, filename, SourceText.From( source ) );

			CompilationOptions compilationOptions = solution
				.GetProject( projectId )!
				.CompilationOptions!
				.WithOutputKind( OutputKind.DynamicallyLinkedLibrary );

			CSharpParseOptions? csharpParseOptions = solution
				.GetProject( projectId )!
				.ParseOptions as CSharpParseOptions;

			if( csharpParseOptions == null ) {
				throw new InvalidOperationException( "CSharp parse options" );
			}

			csharpParseOptions = csharpParseOptions
				.WithLanguageVersion( LanguageVersion.CSharp10 );

			solution = solution
				.WithProjectCompilationOptions( projectId, compilationOptions )
				.WithProjectParseOptions( projectId, csharpParseOptions );

			return solution.GetProject( projectId )!;
		}
	}
}
