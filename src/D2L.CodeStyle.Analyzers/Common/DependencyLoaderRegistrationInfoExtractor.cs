using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common {
	internal sealed class DependencyRegistry {

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

			if( method.IsGenericMethod && method.TypeParameters.Length == 1 && method.Parameters.Length == 1 && arguments.Count == 1 ) {
				// void Register<TDependencyType>( TDependencyType instance );
				// void RegisterPlugin<TDependencyType>( TDependencyType instance );

				var concreteType = semanticModel.GetTypeInfo( arguments[0].Expression ).Type;
				var dependencyType = concreteType;
				if( method.TypeArguments.Length == 1 ) {
					// if there's a type argument provided, use that for dependency type instead
					dependencyType = method.TypeArguments[0];
				}
				return DependencyRegistration.NonFactory( ObjectScope.Singleton, dependencyType, concreteType );
			}

			if( method.Name == "RegisterParentAwareFactory" && method.TypeArguments.Length == 2 && method.Parameters.Length == 0 ) {
				// void RegisterParentAwareFactory<TDependencyType, TFactoryType>();

				return DependencyRegistration.Factory( ObjectScope.AlwaysCreateNewInstance, method.TypeArguments[0], method.TypeArguments[1] );
			}

			if( method.Name == "Register" && !method.IsGenericMethod && method.Parameters.Length == 3 && arguments.Count == 3 ) {
				// void Register( Type dependencyType, Type concreteType, ObjectScope scope );

				// This only handles the case where `typeof` is used inline.
				// This sucks, but there are very few usages of this method (it's
				// only used in open-generic types), so we don't care.
				//		r.Register( typeof(IFoo), typeof(Foo), ObjectScope.Singleton );
				var dependencyTypeExpression = arguments[0].Expression as TypeOfExpressionSyntax;
				if( dependencyTypeExpression == null ) {
					return null;
				}
				var dependencyType = semanticModel.GetSymbolInfo( dependencyTypeExpression.Type ).Symbol as ITypeSymbol;

				var concreteTypeExpression = arguments[1].Expression as TypeOfExpressionSyntax;
				if( dependencyTypeExpression == null ) {
					return null;
				}
				var concreteType = semanticModel.GetSymbolInfo( concreteTypeExpression.Type ).Symbol as ITypeSymbol;

				ObjectScope scope;
				if( !TryGetObjectScope( arguments[2], semanticModel, out scope ) ) {
					return null;
				}

				return DependencyRegistration.NonFactory( scope, dependencyType, concreteType );
			}

			if( method.IsGenericMethod && method.TypeArguments.Length == 2 && method.Parameters.Length == 1 && arguments.Count == 1 ) {
				// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
				// void RegisterPlugin<TDependencyType, TConcreteType>( ObjectScope scope )
				// void RegisterFactory<TDependencyType, TFactoryType>( ObjectScope scope )
				// void RegisterPluginFactory<TDependencyType, TFactoryType>( ObjectScope scope )

				ObjectScope scope;
				if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
					return null;
				}

				if( method.Name.Contains( "Factory" ) ) {
					return DependencyRegistration.Factory( scope, method.TypeArguments[0], method.TypeArguments[1] );
				} else {
					return DependencyRegistration.NonFactory( scope, method.TypeArguments[0], method.TypeArguments[1] );
				}
			}

			// unsupported or incompletely written registration method
			return null;
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
