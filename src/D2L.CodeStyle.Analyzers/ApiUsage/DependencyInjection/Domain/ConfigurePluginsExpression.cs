using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void ConfigurePlugins<TPlugin>( ObjectScope scope )
	// void ConfigureOrderedPlugins<TPlugin, TComparer>( ObjectScope scope ) 
	//		where TComparer : IComparer<TPlugin>, new()
	internal sealed class ConfigurePluginsExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return
				( method.Name == "ConfigurePlugins"
				&& method.TypeParameters.Length == 1
				&& method.Parameters.Length == 1 )
				||
				( method.Name == "ConfigureOrderedPlugins"
				&& method.TypeParameters.Length == 2
				&& method.Parameters.Length == 1 );
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}
			return DependencyRegistration.Marker(
				scope: scope,
				dependencyType: method.TypeArguments[0]
			);
		}
	}
}
