using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class GenericTypeParameterRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			GenericTypeParameterGoal goal
		) {
			// TODO: being constrained by a type that is [Immutable] should
			// make a type parameter safe.
			yield return goal;
		}
	}
}
