using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct FieldGoal : Goal {
		public FieldGoal( IFieldSymbol field ) {
			Field = field;
		}

		public IFieldSymbol Field { get; }
	}
}