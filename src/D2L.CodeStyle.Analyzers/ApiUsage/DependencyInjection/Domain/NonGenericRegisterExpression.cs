using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void Register( Type dependencyType, Type concreteType, ObjectScope scope );
	internal sealed class NonGenericRegisterExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "Register"
				&& !method.IsGenericMethod
				&& method.Parameters.Length == 3;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 3 ) {
				return null;
			}

			// This only handles the case where `typeof` is used inline.
			// This sucks, but there are very few usages of this method (it's
			// only used in open-generic types), so we don't care.
			//		r.Register( typeof(IFoo), typeof(Foo), ObjectScope.Singleton );
			var dependencyTypeExpression = arguments[0].Expression as TypeOfExpressionSyntax;
			if( dependencyTypeExpression == null ) {
				return null;
			}
			var dependencyType = semanticModel.GetSymbolInfo( dependencyTypeExpression.Type ).Symbol as ITypeSymbol;

			var concreteTypeExpression = arguments[1].Expression as TypeOfExpressionSyntax;
			if( concreteTypeExpression == null ) {
				return null;
			}
			var concreteType = semanticModel.GetSymbolInfo( concreteTypeExpression.Type ).Symbol as ITypeSymbol;

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[2], semanticModel, out scope ) ) {
				return null;
			}

			return DependencyRegistration.NonFactory( scope, dependencyType, concreteType );
		}
	}

}
