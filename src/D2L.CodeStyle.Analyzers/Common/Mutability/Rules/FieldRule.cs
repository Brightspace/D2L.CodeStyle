using System;
using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class FieldRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			FieldGoal goal
		) {
			yield return new ReadOnlyGoal( goal.Field );

			if ( goal.Field.DeclaringSyntaxReferences.Length != 1 ) {
				throw new NotImplementedException(
					"Unhandled scenario: unsual number of decl syntaxes for field: "
					+ goal.Field.DeclaringSyntaxReferences.Length
				);
			}

			var decl = goal.Field
				.DeclaringSyntaxReferences[0]
				.GetSyntax() as VariableDeclaratorSyntax;

			if ( decl == null ) {
				throw new NotImplementedException(
					"Unhandled scenario: couldn't cast to FieldDeclartionSyntax"
				);
			}

			// When we have a variable with an initializer, the initializer's
			// expression's type is often narrower than the declared type of
			// the variable. 
			if ( decl.Initializer != null ) {
				yield return new InitializerGoal(
					goal.Field.Type,
					decl.Initializer.Value
				);
			} else {
				yield return new TypeGoal( goal.Field.Type );
			}
		}
	}
}
