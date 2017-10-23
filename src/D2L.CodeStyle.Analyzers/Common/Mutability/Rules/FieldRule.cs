using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class FieldRule {
		public static IEnumerable<Goal> Apply( FieldGoal goal ) {
			yield return new ReadOnlyGoal( goal.Field );

			if ( goal.Field.DeclaringSyntaxReferences.Length != 1 ) {
				throw new NotImplementedException(
					"Unhandled scenario: unsual number of decl syntaxes for field: "
					+ goal.Field.DeclaringSyntaxReferences.Length
				);
			}

			var decl = goal.Field
				.DeclaringSyntaxReferences[0]
				.GetSyntax() as FieldDeclarationSyntax;

			if ( decl == null ) {
				throw new NotImplementedException(
					"Unhandled scenario: couldn't cast to FieldDeclartionSyntax"
				);
			}

			// When we have a variable with an initializer, the initializer's
			// expression's type is often narrower than the declared type of
			// the variable. So it's always better to consider the initializer
			// when present. Fields can have multiple variables declared, e.g:
			//
			//   private Foo m_x, m_y = new Foo();
			//
			// so we need to be careful. It wouldn't be useful here to look at
			// the initializer for m_y because m_x will necessitate us looking
			// at Foo.

			var initializers = decl.Declaration.Variables
				.Select( d => d.Initializer )
				.ToImmutableArray();

			bool missingAnInitializer = initializers.Any( i => i == null );

			// Give up if any initializer is missing
			if ( missingAnInitializer ) {
				yield return new TypeGoal( goal.Field.Type );
				yield break;
			}

			foreach( var init in initializers ) {
				yield return new InitializerGoal( init.Value );
			}

			// NOTE: we may want to pass the type in the initializer goal.
			// implicit conversions make looking at the initializer in
			// isolation theoretically dangerous. See this issue for more info:
			// https://github.com/Brightspace/D2L.CodeStyle/issues/35
		}
	}
}
