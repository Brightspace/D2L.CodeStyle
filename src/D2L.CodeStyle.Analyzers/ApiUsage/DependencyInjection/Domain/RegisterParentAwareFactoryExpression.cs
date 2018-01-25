using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// void RegisterParentAwareFactory<TDependencyType, TFactoryType>();
	internal sealed class RegisterParentAwareFactoryExpression : DependencyRegistrationExpression {

		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "RegisterParentAwareFactory"
				&& method.TypeArguments.Length == 2
				&& method.Parameters.Length == 0;
		}

		internal override DependencyRegistration GetRegistration(
			IMethodSymbol method,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			SemanticModel semanticModel
		) {
			ITypeSymbol concreteType = GetConstructedTypeOfIFactory(
				semanticModel,
				method.TypeArguments[0],
				method.TypeArguments[1]
			);

			return DependencyRegistration.Factory( 
				ObjectScope.AlwaysCreateNewInstance, 
				method.TypeArguments[0], 
				method.TypeArguments[1],
				concreteType
			);
		}

	}

}
