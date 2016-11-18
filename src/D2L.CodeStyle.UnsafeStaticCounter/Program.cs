using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace D2L.CodeStyle.UnsafeStaticCounter {
	internal sealed class Program {
		const int DEFAULT_MAX_CONCURRENCY = 4;

		internal static int Main( string[] args ) {
			int exitCode = Task.Run( async () => await AsyncMain( args ) )
				.GetAwaiter()
				.GetResult();
			return exitCode;
		}

		private static async Task<int> AsyncMain( string[] args ) {
			try {
				return await AsyncThrowableMain( args );
			} catch( Exception e ) {
				LogException( e );
				return -1;
			}
		}

		private static async Task<int> AsyncThrowableMain( string[] args ) {
			var options = ParseOptions( args );
			var prog = new Counter( options );
			var result = await prog.Run();
			return result;
		}

		private static void LogException( Exception e ) {
			Console.Error.WriteLine( e.ToString() );
		}

		private static Options ParseOptions( IEnumerable<string> args ) {
			string path = null;
			int concurrency = DEFAULT_MAX_CONCURRENCY;
			string outputFile = "statics.json";

			var enumerator = args.GetEnumerator();
			while( enumerator.MoveNext() ) {
				switch( enumerator.Current ) {
					case "-n":
						enumerator.MoveNext();
						concurrency = int.Parse( enumerator.Current );
						break;
					case "-d":
						enumerator.MoveNext();
						path = enumerator.Current;
						break;
					case "-o":
						enumerator.MoveNext();
						outputFile = enumerator.Current;
						break;
					default:
						throw new InvalidOperationException( $"unknown option: {enumerator.Current}" );
				}
			}

			if( string.IsNullOrWhiteSpace( path ) ) {
				throw new InvalidOperationException( "usage: UnsafeStaticsCounter.exe -d {rootDir} [-n {concurrency} -o {outputFile}]" );
			}

			Console.WriteLine( "Using options:" );
			Console.WriteLine( $"\tPath = {path}" );
			Console.WriteLine( $"\tMaxConcurrency = {concurrency}" );
			Console.WriteLine( $"\tOutputFile = {outputFile}" );
			Console.WriteLine();

			return new Options( path, concurrency, outputFile );
		}
	}

	internal sealed class Options {
		public readonly string RootDir;
		public readonly int MaxConcurrency;
		public readonly string OutputFile;
		public Options( string rootDir, int maxConcurrency, string outputFile ) {
			RootDir = rootDir;
			MaxConcurrency = maxConcurrency;
			OutputFile = outputFile;
		}
	}

}
