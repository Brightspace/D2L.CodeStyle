using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

/// <summary>
/// An IEqualityComparer for things paired with a compilation that ignroes the
/// compilation. Used to configure an incremental generator pipeline.
/// </summary>
internal sealed class IgnoreCompilationComparer<T> : IEqualityComparer<(T, Compilation)> {
	public static readonly IgnoreCompilationComparer<T> Instance = new();
	public bool Equals( (T, Compilation) x, (T, Compilation) y )
		=> EqualityComparer<T>.Default.Equals( x.Item1, y.Item1 );

	public int GetHashCode( (T, Compilation) obj )
		=> obj.Item1?.GetHashCode() ?? 0;
}

internal static class IgnoreCompilationComparerExtensions {
	public static IncrementalValuesProvider<(T, Compilation)> WithComparerThatIgnoresCompilation<T>(
		this IncrementalValuesProvider<(T, Compilation)> values
	) => values.WithComparer( IgnoreCompilationComparer<T>.Instance );

	public static IncrementalValueProvider<(T, Compilation)> WithComparerThatIgnoresCompilation<T>(
		this IncrementalValueProvider<(T, Compilation)> values
	) => values.WithComparer( IgnoreCompilationComparer<T>.Instance );
}
