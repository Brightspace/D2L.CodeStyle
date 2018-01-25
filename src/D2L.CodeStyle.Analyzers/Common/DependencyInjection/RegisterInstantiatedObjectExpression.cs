using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void Register<TDependencyType>( TDependencyType instance );
	// void RegisterPlugin<TDependencyType>( TDependencyType instance );
	internal sealed class RegisterInstantiatedObjectExpression : DependencyRegistrationExpression {
		public override bool CanHandleMethod( IMethodSymbol method ) {
			return ( method.Name == "Register" || method.Name == "RegisterPlugin" )
				&& method.IsGenericMethod 
				&& method.TypeParameters.Length == 1 
				&& method.Parameters.Length == 1;
		}

		public override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			var concreteType = semanticModel.GetTypeInfo( arguments[0].Expression ).Type;
			var dependencyType = concreteType;
			if( method.TypeArguments.Length == 1 ) {
				// if there's a type argument provided, use that for dependency type instead
				dependencyType = method.TypeArguments[0];
			}
			if( concreteType.IsNullOrErrorType() ) {
				// concreteType can sometimes legitimately not resolve, like in this case:
				//		Register<IFoo>( null );
				concreteType = dependencyType;
			}
			return DependencyRegistration.NonFactory( ObjectScope.Singleton, dependencyType, concreteType );
		}
	}

}
