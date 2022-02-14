using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal readonly record struct ImmutabilityQuery(
		ImmutableTypeKind Kind,
		ITypeSymbol Type
	) {
		public bool EnforceImmutableTypeParams { get; init; } = true;
	}
}
