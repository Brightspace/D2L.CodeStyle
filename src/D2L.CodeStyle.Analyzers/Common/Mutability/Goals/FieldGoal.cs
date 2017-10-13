using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal sealed class FieldGoal : Goal {
		internal FieldGoal( IFieldSymbol field ) {
			Field = field;	
		}

		public IFieldSymbol Field { get; }
	}
}