using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static D2L.CodeStyle.Analyzers.Spec;

namespace D2L.CodeStyle.SpecTests.Framework {

	public static class SpecTestRunner {

		private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile( typeof( object ).Assembly.Location );
		private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile( typeof( System.Uri ).Assembly.Location );
		private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile( typeof( Enumerable ).Assembly.Location );
		private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile( typeof( CSharpCompilation ).Assembly.Location );
		private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile( typeof( Compilation ).Assembly.Location );
		private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile( Assembly.Load( "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" ).Location );
		private static readonly MetadataReference AnnotationsReference = MetadataReference.CreateFromFile( typeof( Annotations.Because ).Assembly.Location );

		public static async Task<ImmutableArray<Diagnostic>> RunAsync(
				DiagnosticAnalyzer analyzer,
				string source
			) {

			Compilation compilation = await GetCompilationForSourceAsync( specName: "foo", source );

			ImmutableArray<Diagnostic> diagnostics = await GetActualDiagnosticsAsync( compilation, analyzer );
			return diagnostics;
		}

		private static Task<Compilation> GetCompilationForSourceAsync( string specName, string source ) {

			ProjectId projectId = ProjectId.CreateNewId( debugName: specName );
			string filename = specName + ".cs";
			DocumentId documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			Solution solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, specName, specName, LanguageNames.CSharp )

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

		private static async Task<ImmutableArray<Diagnostic>> GetActualDiagnosticsAsync(
				Compilation compilation,
				DiagnosticAnalyzer analyzer
				) {

			ImmutableArray<AdditionalText> additionalFiles = await GetAditionalFilesAsync();

			return await compilation
				.WithAnalyzers(
					analyzers: ImmutableArray.Create( analyzer ),
					options: new AnalyzerOptions( additionalFiles: additionalFiles )
				)
				.GetAnalyzerDiagnosticsAsync();
		}

		private static async Task<ImmutableArray<AdditionalText>> GetAditionalFilesAsync() {

			var additionalFiles = ImmutableArray.CreateBuilder<AdditionalText>();

			Assembly testAssembly = Assembly.GetExecutingAssembly();
			foreach( string resourcePath in testAssembly.GetManifestResourceNames() ) {

				if( !resourcePath.EndsWith( "AllowedList.txt" ) ) {
					continue;
				}

				string allowedListName = Regex.Replace(
					resourcePath,
					@"^.*\.(?<allowedListName>[^\.]*)\.txt$",
					@"${allowedListName}.txt"
				);

				using StreamReader reader = new( testAssembly.GetManifestResourceStream( resourcePath ) );
				string text = await reader.ReadToEndAsync();

				additionalFiles.Add( new AdditionalFile(
					path: allowedListName,
					text: text
				) );
			}

			return additionalFiles.ToImmutable();
		}
	}
}
