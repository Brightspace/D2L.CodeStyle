using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed record DiagnosticExpectation(
		string Name,
		Location Location,
		ImmutableArray<string> MessageArguments
	);
}
