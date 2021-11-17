using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void ConfigurePlugins<TExtensionPoint, TPlugin>( ObjectScope scope )
	//      where TExtensionPoint : IExtensionPoint<TPlugin>
	// void ConfigureOrderedPlugins<TExtensionPoint, TPlugin, TComparer>( ObjectScope scope )
	//      where TExtensionPoint : IExtensionPoint<TPlugin>
	//		where TComparer : IComparer<TPlugin>, new()
	internal sealed class ConfigurePluginsExtensionPointExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return ( method.Name == "ConfigurePlugins" && method.TypeParameters.Length == 2 && method.Parameters.Length == 1 )
				|| ( method.Name == "ConfigureOrderedPlugins" && method.TypeParameters.Length == 3 && method.Parameters.Length == 1 )
			;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			if( !TryGetObjectScope( arguments[0], semanticModel, out ObjectScope scope ) ) {
				return null;
			}

			return DependencyRegistration.Marker(
				scope: scope,
				dependencyType: method.TypeArguments[1]
			);
		}
	}
}
