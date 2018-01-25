using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// 	public static void RegisterPluginExtensionPoint<TExtensionPoint, T>(
	//			this IDependencyRegistry @this,
	//			ObjectScope scope
	//		) where TExtensionPoint : IExtensionPoint<T>;
	internal sealed class RegisterExtensionPointExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "RegisterPluginExtensionPoint"
				&& method.IsExtensionMethod
				&& method.Parameters.Length == 1
				&& method.TypeArguments.Length == 2;
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
				scope,
				method.TypeArguments[1],
				method.TypeArguments[1]
			);
		}
	}
}
