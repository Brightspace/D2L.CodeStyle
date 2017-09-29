using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	internal abstract class DependencyRegistrationExpression {

		internal abstract bool CanHandleMethod(
			IMethodSymbol method
		);

		internal abstract DependencyRegistration GetRegistration(
			IMethodSymbol method, 
			SeparatedSyntaxList<ArgumentSyntax> arguments, 
			SemanticModel semanticModel
		);

		protected bool TryGetObjectScope(
			ArgumentSyntax argument,
			SemanticModel semanticModel,
			out ObjectScope scope
		) {
			scope = ObjectScope.AlwaysCreateNewInstance; // bogus

			var scopeArgumentValue = semanticModel.GetConstantValue( argument.Expression );
			if( !scopeArgumentValue.HasValue ) {
				// this can happen if someone is typing, or in the rare case that someone doesn't pass this value inline (i.e., uses a variable)
				return false;
			}

			// if this cast fails, things explode...but I want it to, because this shouldn't fail
			// unless someone redefines LP's ObjectScope enum to `long` (boxed types aren't coerced)
			scope = (ObjectScope)(int)scopeArgumentValue.Value;
			return true;
		}
	}
}
