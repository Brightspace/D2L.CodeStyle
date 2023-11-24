using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Pinning {

	internal record MustBePinnedType(
		INamedTypeSymbol PinnedAttributeSymbol,
		bool Recursive,
		DiagnosticDescriptor Descriptor,
		DiagnosticDescriptor ParameterShouldBeChangedDescriptor,
		params INamedTypeSymbol[] AlternatePinnedAttributes);

}
