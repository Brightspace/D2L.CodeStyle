using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterPlugin<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	// void RegisterPluginFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	internal sealed class FullyGenericRegisterExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.IsGenericMethod 
				&& method.TypeArguments.Length == 2 
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

			if( method.Name.Contains( "Factory" ) ) {
				return DependencyRegistration.Factory( scope, method.TypeArguments[0], method.TypeArguments[1] );
			} else {
				return DependencyRegistration.NonFactory( scope, method.TypeArguments[0], method.TypeArguments[1] );
			}
		}
	}

}
