using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void ConfigurePlugins<TPlugin>( this IDependencyRegistry registry, ObjectScope scope )
	// void ConfigureOrderedPlugins<TPlugin, TComparer>( this IDependencyRegistry registry, ObjectScope scope ) 
	//		where TComparer : IComparer<TPlugin>, new()
	internal sealed class ConfigurePluginsExpression : DependencyRegistrationExpression {
		public override bool CanHandleMethod( IMethodSymbol method ) {
			return
				( method.Name == "ConfigurePlugins"
				&& method.IsExtensionMethod
				&& method.TypeParameters.Length == 1
				&& method.Parameters.Length == 1 )
				||
				( method.Name == "ConfigureOrderedPlugins"
				&& method.IsExtensionMethod
				&& method.TypeParameters.Length == 2
				&& method.Parameters.Length == 1 );
		}

		public override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}
			return DependencyRegistration.NonFactory(
				scope: scope,
				dependencyType: method.TypeArguments[0],
				concreteType: method.TypeArguments[0]
			);
		}
	}
}
