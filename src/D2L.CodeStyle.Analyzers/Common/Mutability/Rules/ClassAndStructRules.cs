using System;
using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Commmon.Mutability.Rules {
	internal static class ClassAndStructRules {
		public static IEnumerable<Goal> Apply(
			SemanticModel model,
			ClassGoal goal
		) {
			yield return new ConcreteTypeGoal(
				goal.Type.BaseType
			);

			foreach( var obl in ApplyToMembers( model, goal.Type ) ) {
				yield return obl;
			}
		}

		public static IEnumerable<Goal> Apply(
			SemanticModel model,
			StructGoal goal
		) {
			// Structs have no base type

			foreach( var obl in ApplyToMembers( model, goal.Type ) ) {
				yield return obl;
			}
		}

		private static IEnumerable<Goal> ApplyToMembers(
			SemanticModel model,
			ITypeSymbol type
		) {
			// This rule doesn't work for types defined in other assemblies.
			// We shouldn't ever hit the scenario (because these goals are the
			// result of breaking down a ConcreteType goal and that should
			// be handling this case) but the check is because if we did get
			// in this scenario things would mostly work, but iterating the
			// members of this decl would return a subset of the total members
			// in that class/struct. That could result in judging things as
			// safe that aren't.
			if( model.Compilation.Assembly != type.ContainingAssembly ) {
				throw new NotImplementedException();
			}

			var members = type.GetExplicitNonStaticMembers();

			foreach( ISymbol member in members ) {
				Goal obl;
				if( MemberToGoal( member, out obl ) ) {
					yield return obl;
				}
			}
		}

		private static bool MemberToGoal(
			ISymbol member,
			out Goal res
		) {
			switch( member.Kind ) {
				case SymbolKind.Field:
					res = new FieldGoal( member as IFieldSymbol );
					return true;

				case SymbolKind.Property:
					res = new PropertyGoal( member as IPropertySymbol );
					return true;

				case SymbolKind.Method:
					res = null;
					return false;

				default:
					throw new NotImplementedException();
			}
		}
	}
}
