using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests {

	internal static class Diagnostics {

		public static readonly DiagnosticDescriptor SourceGeneratorException = new DiagnosticDescriptor(
			id: "SPEC0001",
			title: "Exception was thrown from source generator",
			messageFormat: "The '{0}' source generator threw an exception of type '{1}': {2}",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
	}
}
