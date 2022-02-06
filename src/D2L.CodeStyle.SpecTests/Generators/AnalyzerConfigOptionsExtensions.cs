using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generators {

	internal static class AnalyzerConfigOptionsExtensions {

		public static string GetRequiredOption(
				this AnalyzerConfigOptions options,
				string key
			) {

			if( !options.TryGetValue( key, out string? value ) ) {
				throw new InvalidOperationException( $"Could not get required '{ key }' analyzer config option." );
			}

			return value;
		}

		public static bool IsAdditionalFileOfKind(
				this AnalyzerConfigOptions options,
				string kind
			) {

			if( !options.TryGetValue( "build_metadata.AdditionalFiles.Kind", out string? value ) ) {
				return false;
			}

			if( !kind.Equals( value, StringComparison.Ordinal ) ) {
				return false;
			}

			return true;
		}
	}
}
