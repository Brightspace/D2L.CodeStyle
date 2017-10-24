using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class ReadOnlyRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			ReadOnlyGoal goal
		) {
			if ( goal.Field != null && goal.Field.IsReadOnly ) {
				yield break;
			}

			if ( goal.Property != null && goal.Property.IsReadOnly ) {
				yield break;
			}

			yield return goal;
		}
	}
}