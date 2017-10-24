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
			// type. We should consider goal.Type and maybe also
			// GetTypeInfo( expr ).ConvertedType to figure out when this is
			// happening. The GitHub issue for this is:
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