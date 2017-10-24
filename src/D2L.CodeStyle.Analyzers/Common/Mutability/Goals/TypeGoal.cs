using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct TypeGoal : Goal {
		public TypeGoal( ITypeSymbol type ) {
			Type = type;
		}

		public ITypeSymbol Type { get; }
	}
}
