using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct GenericTypeParameterGoal : Goal {
		public GenericTypeParameterGoal(
			ITypeParameterSymbol type
		) {
			Type = type;
		}

		public ITypeParameterSymbol Type { get; }
	}
}
