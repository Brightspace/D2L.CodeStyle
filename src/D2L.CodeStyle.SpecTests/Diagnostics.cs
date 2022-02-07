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

		public static readonly DiagnosticDescriptor NoGlobalConfigFile = new DiagnosticDescriptor(
			id: "SPEC0002",
			title: "No global spec test config included",
			messageFormat: "There must be an additional file of kind 'D2L.CodeStyle.SpecTest.GlobalAdditionalFile' included",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor TooManyGlobalConfigFiles = new DiagnosticDescriptor(
			id: "SPEC0003",
			title: "Too many global spec test configs included",
			messageFormat: "There can be only a single additional file of kind 'D2L.CodeStyle.SpecTest.GlobalAdditionalFile' included",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor GlobalConfigFileNotFound = new DiagnosticDescriptor(
			id: "SPEC0004",
			title: "The global config file was not found",
			messageFormat: "The additional file '{0}' of kind 'D2L.CodeStyle.SpecTest.GlobalAdditionalFile' was not found",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor GlobalConfigInvalid = new DiagnosticDescriptor(
			id: "SPEC0005",
			title: "The global config file is invalid",
			messageFormat: "The global config file '{0}' is invalid: {1}",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor GlobalConfigDiagnosticDescriptorSourceRequired = new DiagnosticDescriptor(
			id: "SPEC0006",
			title: "At least one diagnostic descriptor source must be added",
			messageFormat: "At least one diagnostic descriptor source type must be added to '{0}' under the path 'config/diagnosticDescriptorSources'",
			category: "Exception",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
	}
}
