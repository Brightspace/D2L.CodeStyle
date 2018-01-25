using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// 	public static void ConfigureInstancePlugins<TPlugin>(
	//			this IDependencyRegistry registry,
	//			ObjectScope scope
	//		);
	//	public static void ConfigureInstancePlugins<TPlugin, TExtensionPoint>(
	//			this IDependencyRegistry registry,
	//			ObjectScope scope
	//		) where TExtensionPoint : ExtensionPointDescriptor, new();
	internal sealed class ConfigureInstancePluginsExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return
				( method.Name == "ConfigureInstancePlugins"
				&& method.IsExtensionMethod
				&& method.TypeParameters.Length == 1
				&& method.Parameters.Length == 1 )
				||
				( method.Name == "ConfigureInstancePlugins"
				&& method.IsExtensionMethod
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
			return DependencyRegistration.NonFactory(
				scope: scope,
				dependencyType: method.TypeArguments[0],
				concreteType: method.TypeArguments[0]
			);
		}
	}
}
