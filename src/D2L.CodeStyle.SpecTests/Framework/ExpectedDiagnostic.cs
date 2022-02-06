using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed record ExpectedDiagnostic(
		string Alias,
		LinePositionSpan LinePosition,
		ImmutableArray<string> MessageArguments
	);
}
