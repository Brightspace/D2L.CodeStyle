using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Parser {

	internal sealed record class AnalyzerSpec(
		string Name,
		string AnalyzerQualifiedTypeName,
		ImmutableArray<AnalyzerSpec.ExpectedDiagnostic> ExpectedDiagnostics,
		string Source
	) {

		internal sealed record class ExpectedDiagnostic(
			string Alias,
			Location Location,
			ImmutableArray<string> MessageArguments
		);
	}
}
