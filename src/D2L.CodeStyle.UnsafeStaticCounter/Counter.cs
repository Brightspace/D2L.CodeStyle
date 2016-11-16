using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D2L.CodeStyle.Analyzers.UnsafeStatics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class Counter {

		private string _rootDir;
		private string _outputFile;
		ImmutableArray<DiagnosticAnalyzer> _analyzers;
		SemaphoreSlim _semaphore;

		public Counter( Options options ) {
			_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>( new UnsafeStaticsAnalyzer() );
			_semaphore = new SemaphoreSlim( options.MaxConcurrency );
			_rootDir = options.RootDir;
			_outputFile = options.OutputFile;
		}

		internal async Task Run() {

			var projectFiles = Directory.EnumerateFiles( _rootDir, "*.csproj", SearchOption.AllDirectories );

			var tasks = projectFiles.Select( AnalyzeProject );
			var results = await Task.WhenAll( tasks );

			var combinedResult = results
				.SelectMany( r => r )
				.ToArray();

			var finalResult = new AnalyzedResults( combinedResult );

			var serializer = new JsonSerializer();
			using( var file = File.Open( _outputFile, FileMode.Create ) )
			using( var stream = new StreamWriter( file ) ) {
				serializer.Serialize( stream, finalResult );
			}
			Console.WriteLine( "done" );
		}

		async Task<AnalyzedStatic[]> AnalyzeProject( string projectFile ) {
			try {
				_semaphore.Wait();
				using( var workspace = MSBuildWorkspace.Create() ) {
					var proj = await workspace.OpenProjectAsync( projectFile );
					Console.WriteLine( $"Analyzing: ${proj.FilePath}" );

					// ignore projects with analyzer already included -- there are no unsafe statics by definition
					if( ProjectAlreadyAnalyzed( proj)) {
						Console.WriteLine( $"...skipping ${proj.FilePath}" );
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
	}
}
