using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using D2L.CodeStyle.Analyzers.UnsafeStatics;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class Counter {

		private static readonly ImmutableHashSet<Regex> s_assemblyPatternsToIgnore = ImmutableHashSet.Create(
			new Regex( @"D2L\.Automation\.UI.*", RegexOptions.Compiled )
		);
		private const string s_analyzerReferenceName = @"D2L.CodeStyle.Analyzers";

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
			var stopwatch = Stopwatch.StartNew();

			var results = await AnalyzeProjects();
			WriteOutputFile( results );

			stopwatch.Stop();
			Console.WriteLine( $"done in {stopwatch.ElapsedMilliseconds}ms." );
			return 0;
		}

		private async Task<AnalyzedResults> AnalyzeProjects() {
			var projectFiles = GetProjectPaths();

			var tasks = projectFiles.Select( AnalyzeProject );
			var results = await Task.WhenAll( tasks );

			var combinedResult = results
				.Where( r => r != null )
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

		private async Task<AnalyzedProject> AnalyzeProject( string projectFile ) {
			if( ShouldIgnoreProject( projectFile ) ) {
				Console.WriteLine( $"...skipping {projectFile}" );
				return null;
			}

			try {
				_semaphore.Wait();
				using( var workspace = MSBuildWorkspace.Create( _msbuildOptions ) ) {
					var proj = await workspace.OpenProjectAsync( projectFile );
					Console.WriteLine( $"Analyzing: {proj.FilePath}" );

					var compilation = await proj.GetCompilationAsync();

					var isAnalyzed = IsAnalyzed( proj, compilation );

					// if the project doesn't reference Annotations, ignore
					var unauditedAttribute = compilation.GetTypeByMetadataName( typeof( Statics.Unaudited ).FullName );
					if( unauditedAttribute == null ) {
						return new AnalyzedProject( proj.Name, isAnalyzed, new AnalyzedStatic[0] );
					}

					var visitor = new CountingVisitor();
					var members = compilation.GetSymbolsWithName( n => true, SymbolFilter.Member );
					Parallel.ForEach( members, m => m.Accept( visitor ) );

					var statics = visitor.AnalyzedStatics.ToArray();
					return new AnalyzedProject( proj.Name, isAnalyzed, statics );
				}
			} catch( Exception e ) {
				throw new Exception( $"Exception analyzing project {projectFile}", e );
			} finally {
				_semaphore.Release();
			}
		}

		private static bool IsAnalyzed( Project proj, Compilation compilation ) {
			var analyzers = proj.AnalyzerReferences;
			if( !analyzers.Any( r => r.Display == s_analyzerReferenceName ) ) {
				return false;
			}

			var attributes = compilation.Assembly.GetAttributes();
			if( attributes.Any( a => a.AttributeClass.MetadataName == "SuperHackySketchyAssemblyThatIsExemptCuzLikeItsSpecialSnowflake" ) ) {
				return false;
			}

			return true;
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
