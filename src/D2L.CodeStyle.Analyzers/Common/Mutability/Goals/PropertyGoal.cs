using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct PropertyGoal : Goal {
		public PropertyGoal( IPropertySymbol property ) {
			Property = property;
		}

		public IPropertySymbol Property { get; }
	}
}
