using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal struct ImmutabilityQuery {
		public ImmutabilityQuery(
			ImmutableTypeKind kind,
			ITypeSymbol type
		) {
			Kind = kind;
			Type = type;
		}

		public ImmutableTypeKind Kind { get; }
		public ITypeSymbol Type { get; }
	}
}
