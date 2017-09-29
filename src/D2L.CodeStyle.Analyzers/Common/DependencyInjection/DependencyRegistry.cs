using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	internal sealed class DependencyRegistry {

		private static readonly ImmutableArray<DependencyRegistrationExpression> s_registrationExpressions = ImmutableArray.Create<DependencyRegistrationExpression>(
			new RegisterParentAwareFactoryExpression(),
			new RegisterInstantiatedObjectExpression(),
			new NonGenericRegisterExpression(),
			new FullyGenericRegisterExpression()
		);

		private readonly INamedTypeSymbol m_dependencyRegistryType;
		private readonly INamedTypeSymbol m_objectScopeType;

		public DependencyRegistry(
			INamedTypeSymbol objectScopeType,
			INamedTypeSymbol dependencyRegistryType
		) {
			m_objectScopeType = objectScopeType;
			m_dependencyRegistryType = dependencyRegistryType;
		}

		public static bool TryCreateRegistry( Compilation compilation, out DependencyRegistry registry ) {
			var dependencyRegistryType = compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.IDependencyRegistry" );
			if( dependencyRegistryType.IsNullOrErrorType() ) {
				registry = null;
				return false;
			}
			var objectScopeType = compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.ObjectScope" );
			if( objectScopeType.IsNullOrErrorType() ) {
				registry = null;
				return false;
			}

			registry = new DependencyRegistry(
				dependencyRegistryType: dependencyRegistryType,
				objectScopeType: objectScopeType
			);
			return true;
		}

		/// <summary>
		/// Attempts to extract a <see cref="DependencyRegistration"/> from a <code>Register*</code> invocation.
		/// </summary>
		/// <returns>Returns null if the expression is not a registration, or is an unsupported registration.</returns>
		public DependencyRegistration GetRegistration( 
			InvocationExpressionSyntax registrationExpression, 
			SemanticModel semanticModel 
		) {
			var method = semanticModel.GetSymbolInfo( registrationExpression ).Symbol as IMethodSymbol;
			if( method == null ) {
				return null;
			}

			if( method.ContainingType != m_dependencyRegistryType ) {
				return null;
			}

			if( registrationExpression.ArgumentList == null ) {
				return null;
			}
			var arguments = registrationExpression.ArgumentList.Arguments;

			var mappedRegistrationExpression = s_registrationExpressions.FirstOrDefault(
				expr => expr.CanHandleMethod( method )
			);
			if( mappedRegistrationExpression == null ) {
				// todo: raise a diagnostic here eventually, all register calls have to be picked up
				return null;
			}

			var registation = mappedRegistrationExpression.GetRegistration( method, arguments, semanticModel );
			return registation;
		}

		private bool TryGetObjectScope(
			ArgumentSyntax argument,
			SemanticModel semanticModel,
			out ObjectScope scope
		) {
			scope = ObjectScope.AlwaysCreateNewInstance; // bogus

			var scopeArgumentValue = semanticModel.GetConstantValue( argument.Expression );
			if( !scopeArgumentValue.HasValue ) {
				// this can happen if someone is typing, or in the rare case that someone doesn't pass this value inline (i.e., uses a variable)
				return false;
			}

			// if this cast fails, things explode...but I want it to, because this shouldn't fail
			// unless someone redefines LP's ObjectScope enum to `long` (boxed types aren't coerced)
			scope = (ObjectScope)(int)scopeArgumentValue.Value;
			return true;
		}
	}
}
