using System;
using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class PropertyRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			PropertyGoal goal
		) {
			var syntax = GetSyntax( goal.Property );

			// Properties that are auto-implemented have an implicit backing
			// field that may be mutable. Otherwise, properties are just sugar
			// for getter/setter methods and don't themselves contribute to
			// mutability.
			if ( !syntax.IsAutoImplemented() ) {
				yield break;
			}

			yield return new ReadOnlyGoal( goal.Property );

			if ( syntax.Initializer != null ) {
				yield return new InitializerGoal(
					goal.Property.Type,
					syntax.Initializer.Value
				);
			} else {
				yield return new TypeGoal( goal.Property.Type );
			}

			// TODO: investigate if we can have IsAutoProperty without delving
			// into the syntax. We need to get the syntax anyway for the 
			// initializer, though.
		}

		private static PropertyDeclarationSyntax GetSyntax(
			IPropertySymbol property
		) {
			var decls = property.DeclaringSyntaxReferences;

			if ( decls.Length != 1 ) {
				throw new NotImplementedException(
					"Unexpected number of decls for property: "
					+ decls.Length
				);
			}

			var decl = decls[0].GetSyntax() as PropertyDeclarationSyntax;

			if (decl == null ) {
				throw new NotImplementedException(
					"Unexpectedly failed to cast to PropertyDeclarationSyntax"
				);
			}

			return decl;
		}
	}
}
