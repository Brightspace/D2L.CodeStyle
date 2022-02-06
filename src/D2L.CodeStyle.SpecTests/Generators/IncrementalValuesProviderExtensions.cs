using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Generators {

	internal static class IncrementalValuesProviderExtensions {

		public static IncrementalValuesProvider<T> WhereNotNull<T>(
				this IncrementalValuesProvider<T?> source
			) {

			return source.Where( static m => m is not null )!;
		}
	}
}
