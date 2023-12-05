using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	internal record DeserializableTypeInfo(
		INamedTypeSymbol MustBeDeserializableAttribute,
		DiagnosticDescriptor Descriptor,
		DiagnosticDescriptor ParameterShouldBeChangedDescriptor,
		params INamedTypeSymbol[] ValidAttributes );

}
