using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterPlugin<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	// void RegisterPluginFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	internal sealed class FullyGenericRegisterExpression : DependencyRegistrationExpression {

		private static readonly ImmutableHashSet<string> s_validNames = ImmutableHashSet.Create(
			"Register",
			"RegisterPlugin",
			"RegisterFactory",
			"RegisterPluginFactory"
		);

		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return s_validNames.Contains( method.Name )
				&& method.IsGenericMethod 
				&& method.TypeArguments.Length == 2 
				&& method.Parameters.Length == 1;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			if( method.Name.Contains( "Factory" ) ) {
				return GetFactoryRegistration( method, arguments, semanticModel );
			}

			return GetNonFactoryRegistration( method, arguments, semanticModel );
		}

		private DependencyRegistration GetNonFactoryRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}

			return DependencyRegistration.NonFactory( 
				scope, 
				method.TypeArguments[0], 
				method.TypeArguments[1] 
			);
		}

		private DependencyRegistration GetFactoryRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}

			ITypeSymbol concreteType = GetConstructedTypeOfIFactory(
				semanticModel,
				method.TypeArguments[0],
				method.TypeArguments[1]
			);

			return DependencyRegistration.Factory(
				scope,
				dependencyType: method.TypeArguments[0],
				factoryType: method.TypeArguments[1],
				concreteType: concreteType
			);
		}
	}
}
