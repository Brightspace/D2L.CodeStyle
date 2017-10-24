using System;
using System.Collections.Generic;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	internal static class ConcreteTypeRule {
		public static IEnumerable<Goal> Apply(
			ISemanticModel model,
			ConcreteTypeGoal goal
		) {
			switch ( goal.Type.TypeKind ) {
				case TypeKind.Array:
				case TypeKind.Delegate:
				case TypeKind.Dynamic:
					// These objects are inherently mutable
					yield return goal;
					yield break;

				case TypeKind.Enum:
					// Enums don't hold mutable state
					yield break;

				case TypeKind.Error:
					// This only happens if there are other errors in the
					// compilation. We ignore these types to avoid creating 
					// additional unhelpful diagnostics.
					yield break;

				case TypeKind.Class:
					yield return new ClassGoal( goal.Type );
					yield break;

				case TypeKind.Struct: // equivalent to TypeKind.Structure (VB)
					yield return new StructGoal( goal.Type );
					yield break;

				case TypeKind.TypeParameter:
					yield return new GenericTypeParameterGoal( goal.Type as ITypeParameterSymbol );
					yield break;

				case TypeKind.Interface:
					throw new InvalidOperationException( "Unexpected ConcreteType goal for interface type" );

				case TypeKind.Module:
				case TypeKind.Pointer:
				case TypeKind.Submission:
				case TypeKind.Unknown:
				default:
					throw new NotImplementedException( "Unhandled case in ConcreteType rule" );
			}
		}
	}
}
