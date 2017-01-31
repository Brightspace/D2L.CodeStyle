using System;
using System.Collections.Generic;
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
		private readonly ImmutableDictionary<string, string> _msbuildOptions;

		public Counter( Options options ) {
			_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>( new UnsafeStaticsAnalyzer() );
			_semaphore = new SemaphoreSlim( options.MaxConcurrency );
			_rootDir = options.RootDir;
			_outputFile = options.OutputFile;
			_msbuildOptions = new Dictionary<string, string> {
				{ "ReferencePath", options.BinDir },
				{ "OutputPath", options.BinDir }
			}.ToImmutableDictionary();
		}

		internal async Task<int> Run() {
			var results = await AnalyzeProjects();
			WriteOutputFile( results );
			Console.WriteLine( "done" );
			return 0;
		}

		private async Task<AnalyzedResults> AnalyzeProjects() {
			var projectFiles = GetProjectPaths();

			var tasks = projectFiles.Select( AnalyzeProject );
			var results = await Task.WhenAll( tasks );

			var combinedResult = results
				.SelectMany( r => r )
				.ToArray();

			var finalResult = new AnalyzedResults( combinedResult );
			return finalResult;
		}

		private IEnumerable<string> GetProjectPaths() {
			if( Directory.Exists( _rootDir ) ) {
				return Directory.EnumerateFiles( _rootDir, "*.csproj", SearchOption.AllDirectories );
			} else if ( File.Exists( _rootDir ) ) {
				return File.ReadAllLines( _rootDir );
			} else {
				throw new Exception( $"File or directory does not exist: '{_rootDir}'" );
			}
		}

		private void WriteOutputFile( AnalyzedResults results ) {
			var serializer = JsonSerializer.Create( new JsonSerializerSettings {
				Formatting = Formatting.Indented
			} );
			using( var file = File.Open( _outputFile, FileMode.Create ) )
			using( var stream = new StreamWriter( file ) ) {
				serializer.Serialize( stream, results );
			}
		}

		private async Task<AnalyzedStatic[]> AnalyzeProject( string projectFile ) {
			if( ShouldIgnoreProject( projectFile ) ) {
				Console.WriteLine( $"...skipping {projectFile}" );
				return new AnalyzedStatic[0];
			}

			try {
				_semaphore.Wait();
				using( var workspace = MSBuildWorkspace.Create( _msbuildOptions ) ) {
					var proj = await workspace.OpenProjectAsync( projectFile );
					Console.WriteLine( $"Analyzing: {proj.FilePath}" );

					// ignore projects with analyzer already included -- there are no unsafe statics by definition
					if( ProjectAlreadyAnalyzed( proj ) ) {
						Console.WriteLine( $"...skipping {proj.FilePath}" );
						return new AnalyzedStatic[0];
					}

					var compilation = await proj.GetCompilationAsync();
					var compilationWithAnalzer = compilation.WithAnalyzers( _analyzers );

					var diags = await compilationWithAnalzer.GetAnalyzerDiagnosticsAsync();
					foreach( var diag in diags ) {
						if( diag.Id != UnsafeStaticsAnalyzer.DiagnosticId ) {
							var location = diag.Location.SourceTree?.FilePath ?? "no location";
							throw new Exception( $"error processing project {proj.Name} ({location}): {diag}" );
						}
					}

					return diags
						.Select( d => new AnalyzedStatic( proj.Name, d ) )
						.ToArray();
				}
			} catch( Exception e ) {
				throw new Exception( $"Exception analyzing project {projectFile}", e );
			} finally {
				_semaphore.Release();
			}
		}

		private static bool ProjectAlreadyAnalyzed( Project proj ) {
			var analyzers = proj.AnalyzerReferences
				.SelectMany( r => r.GetAnalyzers( LanguageNames.CSharp ) );

			// we use the name because UnsafeStaticsAnalyzer is not assembly neutral, so `is` might not work
			if( analyzers.Any( a => a.GetType().Name == nameof( UnsafeStaticsAnalyzer ) ) ) {
				return true;
			}

			return false;
		}

		private static bool ShouldIgnoreProject( string csProjFile ) {
			foreach( var pattern in s_assemblyPatternsToIgnore ) {
				if( pattern.IsMatch( csProjFile ) ) {
					return true;
				}
			}

			return false;
		}
	}
}
