#nullable enable

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace D2L.CodeStyle.Analyzers.Async.Generator {
	/// <summary>
	/// Given a stream of input source paths generate a nice looking set of
	/// distinct hint names for the source generator.
	/// </summary>
	internal sealed class SourceFilePathHintNameGenerator {
		private readonly ImmutableArray<string> m_sourcePaths;
		public readonly int m_prefixLength;

		private SourceFilePathHintNameGenerator(
			ImmutableArray<string> sourcePaths,
			int prefixLength
		) {
			m_sourcePaths = sourcePaths;
			m_prefixLength = prefixLength;
		}

		public static SourceFilePathHintNameGenerator Create( ImmutableArray<string> sourcePaths ) {
			// Calculate the longest prefix of all input strings

			if( sourcePaths.Length == 0 ) {
				return new SourceFilePathHintNameGenerator( sourcePaths, 0 );
			}

			// Seed it with the first source files directory
			string commonPrefix = RemoveFileName( sourcePaths[0] );

			foreach( var sourcePath in sourcePaths.Select( RemoveFileName ) ) {
				int idx = 0;
				int maxPrefixLength = Math.Min( sourcePath.Length, commonPrefix.Length );

				while( idx < maxPrefixLength ) {
					if( sourcePath[idx] != commonPrefix[idx] ) {
						break;
					}

					idx++;
				}

				if( idx != commonPrefix.Length ) {
					commonPrefix = sourcePath.Substring( 0, idx );
				}
			}

			return new SourceFilePathHintNameGenerator(
				sourcePaths,
				commonPrefix.Length
			);
		}

		private static string RemoveFileName( string sourcePath ) {
			var lastSlashIdx = Math.Max(
				sourcePath.LastIndexOf( '/' ),
				sourcePath.LastIndexOf( '\\' )
			);

			if( lastSlashIdx == -1 ) {
				return sourcePath;
			}

			return sourcePath.Substring( 0, lastSlashIdx + 1 );
		}

		private static readonly Regex PathSeparators = new(
			@"[/\\]",
			RegexOptions.Compiled
		);

		private static readonly Regex DisallowedPathChars = new(
			@"[^a-zA-Z0-9-._]*",
			RegexOptions.Compiled
		);

		public IEnumerable<string> GetHintNames() {
			// Hint names need to be unique, but the sanitization we could do
			// may introduce ambiguities.
			var used = new HashSet<string>();

			foreach( var sourcePath in m_sourcePaths ) {
				var sanitized = PathSeparators.Replace(
					sourcePath.Substring( m_prefixLength ),
					"_"
				);

				sanitized = DisallowedPathChars.Replace(
					sanitized,
					""
				);

				if( sanitized.EndsWith( ".cs") ) {
					sanitized = sanitized.Substring( 0, sanitized.Length - 3 );
				}

				var candidate = sanitized;
				var attempt = 0;

				while( used.Contains( candidate ) ) {
					candidate = sanitized + attempt;
					attempt++;
				}

				used.Add( candidate );

				yield return candidate;
			}
		}
	}
}
