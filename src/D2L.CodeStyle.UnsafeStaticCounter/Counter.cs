using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using D2L.CodeStyle.Analyzers.UnsafeStatics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class Counter {

		private static readonly ImmutableHashSet<Regex> s_assemblyPatternsToIgnore = ImmutableHashSet.Create(
			new Regex( @"D2L\.Automation\.UI.*", RegexOptions.Compiled )
		);

		private readonly string _rootDir;
		private readonly string _outputFile;
		private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
		private readonly SemaphoreSlim _semaphore;

		public Counter( Options options ) {
			_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>( new UnsafeStaticsAnalyzer() );
			_semaphore = new SemaphoreSlim( options.MaxConcurrency );
			_rootDir = options.RootDir;
			_outputFile = options.OutputFile;
		}

		internal async Task<int> Run() {
			var results = await AnalyzeProjects();
			WriteOutputFile( results );
			Console.WriteLine( "done" );
			return 0;
		}

		async Task<AnalyzedResults> AnalyzeProjects() {
			var projectFiles = Directory.EnumerateFiles( _rootDir, "*.csproj", SearchOption.AllDirectories );

			var tasks = projectFiles.Select( AnalyzeProject );
			var results = await Task.WhenAll( tasks );

			var combinedResult = results
				.SelectMany( r => r )
				.ToArray();

			var finalResult = new AnalyzedResults( combinedResult );
			return finalResult;
		}

		void WriteOutputFile( AnalyzedResults results ) {
			var serializer = JsonSerializer.Create( new JsonSerializerSettings {
				Formatting = Formatting.Indented
			} );
			using( var file = File.Open( _outputFile, FileMode.Create ) )
			using( var stream = new StreamWriter( file ) ) {
				serializer.Serialize( stream, results );
			}
		}

		async Task<AnalyzedStatic[]> AnalyzeProject( string projectFile ) {
			if( ShouldIgnoreProject( projectFile)) {
				Console.WriteLine( $"...skipping {projectFile}" );
				return new AnalyzedStatic[0];
			}

			try {
				_semaphore.Wait();
				using( var workspace = MSBuildWorkspace.Create() ) {
					var proj = await workspace.OpenProjectAsync( projectFile );
					Console.WriteLine( $"Analyzing: {proj.FilePath}" );

					// ignore projects with analyzer already included -- there are no unsafe statics by definition
					if( ProjectAlreadyAnalyzed( proj)) {
						Console.WriteLine( $"...skipping {proj.FilePath}" );
						return new AnalyzedStatic[0];
					}

					var compilation = await proj.GetCompilationAsync();
					var compilationWithAnalzer = compilation.WithAnalyzers( _analyzers );

					var diags = await compilationWithAnalzer.GetAnalyzerDiagnosticsAsync();
					return diags
						.Select( d => new AnalyzedStatic( proj.Name, d ) )
						.ToArray();
				}
			} finally {
				_semaphore.Release();
			}
		}

		static bool ProjectAlreadyAnalyzed( Project proj ) {
			var analyzers = proj.AnalyzerReferences
				.SelectMany( r => r.GetAnalyzers( LanguageNames.CSharp ) );

			// we use the name because UnsafeStaticsAnalyzer is not assembly neutral, so `is` might not work
			if( analyzers.Any( a => a.GetType().Name == nameof( UnsafeStaticsAnalyzer ) ) ) {
				return true;
			}

			return false;
		}

		static bool ShouldIgnoreProject( string csProjFile ) {
			if( s_assemblyPatternsToIgnore.Any( p => p.IsMatch( csProjFile) ) ) {
				return true;
			}

			return false;
		}
	}
}
