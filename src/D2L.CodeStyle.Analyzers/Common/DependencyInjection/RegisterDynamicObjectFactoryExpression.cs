using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void RegisterDynamicObjectFactory<TOutput, TConcrete, TArg>(
	//		this IDependencyRegistry registry,
	//		ObjectScope scope
	// ) where TConcrete : class, TOutput
	// void RegisterDynamicObjectFactory<TOutput, TConcrete, TArg0, TArg1>(
	//		this IDependencyRegistry registry,
	//		ObjectScope scope
	// ) where TConcrete : class, TOutput
	internal sealed class RegisterDynamicObjectFactoryExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "RegisterDynamicObjectFactory"
				&& method.IsExtensionMethod
				&& ( method.TypeArguments.Length == 3 || method.TypeArguments.Length == 4 )
				&& method.Parameters.Length == 1;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}
			return DependencyRegistration.Factory(
				scope: scope,
				dependencyType: method.TypeArguments[0],
				// the real factory is emitted at runtime, 
				// we don't know what it is at compile time
				factoryType: null
			);
		}
	}
}
