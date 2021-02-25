using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void MarkInterfaceAsPlugin<TDepdendencyType>( this IDependencyRegistry registry, ObjectScope scope )
	//	where TDepdendencyType : class
	internal sealed class MarkInterfaceAsPluginExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "MarkInterfaceAsPlugin"
				&& method.IsGenericMethod
				&& method.TypeArguments.Length == 1
				&& method.Parameters.Length == 1;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[ 0 ], semanticModel, out scope ) ) {
				return null;
			}
			return DependencyRegistration.Marker(
				scope: scope,
				dependencyType: method.TypeArguments[ 0 ]
			);
		}
	}
}
