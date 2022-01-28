using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.TestAnalyzers.Common {
	public static class Diagnostics {

		// Retired:
		// D2LTESTS001 (TestCaseSourceStrings): "Use nameof in TestCaseSource attributes"
		// D2LTESTS002 (ValueSourceStrings): "Use nameof in ValueSource attributes."

		public static readonly DiagnosticDescriptor ConfigTestSetupStrings = new DiagnosticDescriptor(
			id: "D2LTESTS003",
			title: "Use nameof in ConfigTestSetup attributes.",
			messageFormat: "String arguments in ConfigTestSetup are not allowed. Use nameof({0}) instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Using strings in ConfigTestSetup attributes creates false positives during dead code analysis. nameof should be used instead."
		);

		public static readonly DiagnosticDescriptor NUnitCategory = new DiagnosticDescriptor(
			id: "D2LTESTS004",
			title: "Test is incorrectly categorized",
			messageFormat: "Test is incorrectly categorized: {0}",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Tests need to be correctly categorized in order to be run."
		);

		public static readonly DiagnosticDescriptor CustomServiceLocator = new DiagnosticDescriptor(
			id: "D2LTESTS005",
			title: "Use the default test service locator.",
			messageFormat: "Custom service locators should not be used. Use static TestServiceLocator.Get<T>() or TestServiceLocatorFactory.Default instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Custom service locators are expensive to instantiate and slow down tests significantly. Use the default locator instead."
		);

        public static readonly DiagnosticDescriptor TestAttributeMissed = new DiagnosticDescriptor(
            id: "D2LTESTS006",
            title: "Method not labelled as [Test], [Theory], [TestCase], or [TestCaseSource]",
            messageFormat: "Method {0} is public, but does not have [Test], [Theory], [TestCase], or [TestCaseSource] attribute, add attribute or change the method visibility.",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "If a [Test], [Theory], [TestCase], or [TestCaseSource] attribute is missed the test will not be run, leading to false confidence in the code."
        );

        public static readonly DiagnosticDescriptor UnnecessaryAllowedListEntry = new DiagnosticDescriptor(
            id: "D2LTESTS007",
            title: "Unnecessarily listed in an analyzer allowed list",
            messageFormat: "The entry for {0} in {1} is unnecessary.",
            category: "Cleanliness",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Unnecessarily listed in an analyzer allowed list."
        );
    }
}
