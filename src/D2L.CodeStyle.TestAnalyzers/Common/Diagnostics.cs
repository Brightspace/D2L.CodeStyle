using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.TestAnalyzers.Common {
	public static class Diagnostics {
		public static readonly DiagnosticDescriptor TestCaseSourceStrings = new DiagnosticDescriptor(
			id: "D2LTESTS001",
			title: "Use nameof in TestCaseSource attributes.",
			messageFormat: "String arguments in TestCaseSource not allowed. Use nameof( {0} ) instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Using strings in TestCaseSource attributes creates false positives during dead code analysis. nameof should be used instead."
		);

	}
}
