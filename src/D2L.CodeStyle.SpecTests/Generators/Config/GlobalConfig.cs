using System.Collections.Immutable;

namespace D2L.CodeStyle.SpecTests.Generators.Config {

	internal sealed record class GlobalConfig(
		ImmutableArray<string> DiagnosticDescriptorSourceTypes,
		ImmutableArray<string> ReferenceAssemblies
	);
}
