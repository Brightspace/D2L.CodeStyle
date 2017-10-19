using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct ConcreteTypeGoal : Goal {
		public ConcreteTypeGoal( ITypeSymbol type ) {
			Type = type;
		}

		public ITypeSymbol Type { get; }
	}
}
