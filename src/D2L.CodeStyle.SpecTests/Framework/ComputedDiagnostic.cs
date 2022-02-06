using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed record ComputedDiagnostic(
		string? Alias,
		string Id,
		LinePositionSpan LinePosition,
		string Message
	);
}
