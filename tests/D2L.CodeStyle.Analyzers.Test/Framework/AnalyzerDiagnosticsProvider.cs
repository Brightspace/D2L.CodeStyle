using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public static class AnalyzerDiagnosticsProvider {

		private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile( typeof( object ).Assembly.Location );
		private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile( typeof( System.Uri ).Assembly.Location );
		private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile( typeof( Enumerable ).Assembly.Location );
		private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile( typeof( CSharpCompilation ).Assembly.Location );
		private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile( typeof( Compilation ).Assembly.Location );
		private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile( Assembly.Load( "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" ).Location );
		private static readonly MetadataReference AnnotationsReference = MetadataReference.CreateFromFile( typeof( Annotations.Because ).Assembly.Location );

		public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
				DiagnosticAnalyzer analyzer,
				ImmutableArray<AdditionalText> additionalFiles,
				string debugName,
				string source
			) {

			Compilation compilation = await GetCompilationForSourceAsync( debugName, source );

			ImmutableArray<Diagnostic> diagnostics = await compilation
				.WithAnalyzers(
					analyzers: ImmutableArray.Create( analyzer ),
					options: new AnalyzerOptions( additionalFiles )
				)
				.GetAnalyzerDiagnosticsAsync();

			return diagnostics;
		}

		private static Task<Compilation> GetCompilationForSourceAsync(
				string debugName,
				string source
			) {

			ProjectId projectId = ProjectId.CreateNewId( debugName );
			string filename = debugName + ".cs";
			DocumentId documentId = DocumentId.CreateNewId( projectId, debugName: debugName );

			Solution solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, debugName, debugName, LanguageNames.CSharp )

				.AddMetadataReference( projectId, CorlibReference )
				.AddMetadataReference( projectId, SystemReference )
				.AddMetadataReference( projectId, SystemCoreReference )
				.AddMetadataReference( projectId, CSharpSymbolsReference )
				.AddMetadataReference( projectId, CodeAnalysisReference )
				.AddMetadataReference( projectId, SystemRuntimeReference )
				.AddMetadataReference( projectId, AnnotationsReference )

				.AddDocument( documentId, filename, SourceText.From( source ) );

			CompilationOptions compilationOptions = solution
				.GetProject( projectId )
				.CompilationOptions
				.WithOutputKind( OutputKind.DynamicallyLinkedLibrary );

			CSharpParseOptions parseOptions = solution
				.GetProject( projectId )
				.ParseOptions as CSharpParseOptions;

			parseOptions = parseOptions
				.WithLanguageVersion( LanguageVersion.CSharp10 );

			solution = solution
				.WithProjectCompilationOptions( projectId, compilationOptions )
				.WithProjectParseOptions( projectId, parseOptions );

			return solution.Projects.First().GetCompilationAsync();
		}
	}
}
