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

		public static readonly DiagnosticDescriptor ValueSourceStrings = new DiagnosticDescriptor(
			id: "D2LTESTS002",
			title: "Use nameof in ValueSource attributes.",
			messageFormat: "String arguments in ValueSource not allowed. Use nameof( {0} ) instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Using strings in ValueSource attributes creates false positives during dead code analysis. nameof should be used instead."
		);

		public static readonly DiagnosticDescriptor ConfigTestSetupStrings = new DiagnosticDescriptor(
			id: "D2LTESTS003",
			title: "Use nameof in ConfigTestSetup attributes.",
			messageFormat: "String arguments in ConfigTestSetup are not allowed. Use nameof({0}) instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Using strings in ConfigTestSetup attributes creates false positives during dead code analysis. nameof should be used instead."
		);
	}
}
