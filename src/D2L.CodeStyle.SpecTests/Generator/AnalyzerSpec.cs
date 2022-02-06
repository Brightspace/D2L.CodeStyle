using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Generator {

	internal sealed record AnalyzerSpec(
		string AnalyzerQualifiedTypeName,
		ImmutableArray<AnalyzerSpec.ExpectedDiagnostic> ExpectedDiagnostics,
		string Source
	) {

		internal sealed record class ExpectedDiagnostic(
			string Name,
			Location Location,
			ImmutableArray<string> MessageArguments
		);
	}
}
