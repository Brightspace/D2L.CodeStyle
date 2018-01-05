using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	// void RegisterParentAwareFactory<TDependencyType, TFactoryType>();
	internal sealed class RegisterParentAwareFactoryExpression : DependencyRegistrationExpression {

		public override bool CanHandleMethod( IMethodSymbol method ) {
			return method.Name == "RegisterParentAwareFactory"
				&& method.TypeArguments.Length == 2
				&& method.Parameters.Length == 0;
		}

		public override DependencyRegistration GetRegistration(
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
