using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterPlugin<TDependencyType, TConcreteType>( ObjectScope scope )
	// void RegisterFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	// void RegisterPluginFactory<TDependencyType, TFactoryType>( ObjectScope scope )
	internal sealed class FullyGenericRegisterExpression : DependencyRegistrationExpression {
		private const string IFactoryTypeMetadataName = "D2L.LP.Extensibility.Activation.Domain.IFactory`1";

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

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}

			if( method.Name.Contains( "Factory" ) ) {
				ITypeSymbol concreteType = null;
				if( !TryGetConstructedTypeOfIFactory( method.TypeArguments[1], out concreteType ) ) {
					concreteType = method.TypeArguments[1];
				}
				return DependencyRegistration.Factory( scope, method.TypeArguments[0], concreteType );
			}

			return DependencyRegistration.NonFactory( scope, method.TypeArguments[0], method.TypeArguments[1] );
		}

		private bool TryGetConstructedTypeOfIFactory( ITypeSymbol factoryType, out ITypeSymbol constructedType ) {
			constructedType = null;

			var iFactoryType = compilation.GetTypeByMetadataName( IFactoryTypeMetadataName );
			if( !factoryType.ConstructedFrom == iFactoryType ) {
				return false
			}

			if( factoryType.TypeArguments.Length == 0 ) {
				return false;
			}

			constructedType = factoryType.TypeArguments[0];
			return constructedType != null;
		}
	}
}
