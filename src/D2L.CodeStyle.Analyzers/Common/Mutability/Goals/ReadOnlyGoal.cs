using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct ReadOnlyGoal : Goal {
		public ReadOnlyGoal( IPropertySymbol property ) {
			Property = property;
			Field = null;
		}

		public ReadOnlyGoal( IFieldSymbol field ) {
			Property = null;
			Field = field;
		}

		public IPropertySymbol Property { get; }
		public IFieldSymbol Field { get; }
	}
}
