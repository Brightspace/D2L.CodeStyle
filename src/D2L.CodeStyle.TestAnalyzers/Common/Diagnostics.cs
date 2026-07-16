using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.TestAnalyzers.Common {
	public static class Diagnostics {

		// Retired:
		// D2LTESTS001 (TestCaseSourceStrings): "Use nameof in TestCaseSource attributes"
		// D2LTESTS002 (ValueSourceStrings): "Use nameof in ValueSource attributes."

		public static readonly DiagnosticDescriptor ConfigTestSetupStrings = new DiagnosticDescriptor(
			id: "D2LTESTS003",
			title: "Use nameof in ConfigTestSetup attributes",
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
			description: "Tests need to be correctly categorized in order to be run.",
			customTags: new[] { "CompilationEnd" }
		);

		public static readonly DiagnosticDescriptor CustomServiceLocator = new DiagnosticDescriptor(
			id: "D2LTESTS005",
			title: "Use the default test service locator",
			messageFormat: "Custom service locators should not be used. Use static TestServiceLocator.Get<T>() or TestServiceLocatorFactory.Default instead.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Custom service locators are expensive to instantiate and slow down tests significantly. Use the default locator instead."
		);

        // Retired:
        // D2LTESTS006 (TestAttributeMissed): "Method not labelled as [Test], [Theory], [TestCase], or [TestCaseSource]"

        public static readonly DiagnosticDescriptor UnnecessaryAllowedListEntry = new DiagnosticDescriptor(
            id: "D2LTESTS007",
            title: "Unnecessarily listed in an analyzer allowed list",
            messageFormat: "The entry for {0} in {1} is unnecessary",
            category: "Cleanliness",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Unnecessarily listed in an analyzer allowed list.",
            customTags: new[] { "CompilationEnd" }
        );

		public static readonly DiagnosticDescriptor NonConstantPassedToConstantParameter = new DiagnosticDescriptor(
			id: "D2L0074",
			title: "Constant parameter cannot be passed a non-constant value",
			messageFormat: "The \"{0}\" parameter is marked with the [Constant] attribute, and so it must be passed a compile-time constant value",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The method being called has declared that this parameter must receive a constant, but a non-constant value is being passed."
		);

		public static readonly DiagnosticDescriptor InvalidConstantType = new DiagnosticDescriptor(
			id: "D2L0075",
			title: "Invalid data type marked as [Constant]",
			messageFormat: "The [Constant] attribute cannot be used on \"{0}\" types, because they cannot be compile-time constants",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor ReferenceToMethodWithConstantParameterNotSupport = new DiagnosticDescriptor(
			id: "D2L0102",
			title: "References to methods with parameters marked as [Constant] is not supported",
			messageFormat: "References to methods with parameters marked as [Constant] is currently not supported",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor UnexpectedNumberOfParametersForImplicitOperator = new DiagnosticDescriptor(
			id: "D2L0104",
			title: "Unexpected number of parameters for implicit operator",
			messageFormat: "The implicit operator has an unexpected number of parameters. Update analyzer to handle this case.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
	}
}
