using System.Collections.Immutable;

namespace D2L.CodeStyle.SpecTests.Framework {

	public readonly record struct DiagnosticsComparison(
		ImmutableHashSet<ComputedDiagnostic> Matched,
		ImmutableHashSet<ComputedDiagnostic> Missing,
		ImmutableHashSet<ComputedDiagnostic> Unexpected
	);
}
