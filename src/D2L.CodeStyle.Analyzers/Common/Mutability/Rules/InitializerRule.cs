using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class InitializerRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			InitializerGoal goal
		) {
			// TODO: this type may get implicitly converted into an unsafe
			// type. It may be correct t
			// instead but we need to investigate that. See this GH issue:
			// https://github.com/Brightspace/D2L.CodeStyle/issues/35
			var exprType = model.GetTypeForSyntax( goal.Expr );

			if ( goal.Expr is ObjectCreationExpressionSyntax ) {
				yield return new ConcreteTypeGoal( exprType );
				yield break;
			}

			yield return new TypeGoal( exprType );
		}
	}
}