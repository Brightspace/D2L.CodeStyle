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

		public static readonly DiagnosticDescriptor MisusedAssertIsTrueOrFalse = new DiagnosticDescriptor(
			id: "D2LTESTS003",
			title: "Use appropriate Assert method, instead of Assert.IsTrue/IsFalse.",
			messageFormat: "Invoking {0} with a boolean expression is strongly discourraged: use {1} instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Using appropriate Assert methods yield better diagnostic messages in tests."
		);
	}
}
