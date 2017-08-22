using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common {
	internal sealed class DependencyRegistry {

		private readonly INamedTypeSymbol m_dependencyRegistryType;
		private readonly INamedTypeSymbol m_objectScopeType;
		private readonly int m_objectScopeSingletonEnumValue;

		// void Register<TDependencyType>( TDependencyType instance );
		private readonly IMethodSymbol m_registerConcreteObjectMethod;

		// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
		private readonly IMethodSymbol m_registerWithDependencyTypeAndConcreteTypeMethod;

		// void Register( Type dependencyType, Type concreteType, ObjectScope scope );
		private readonly IMethodSymbol m_registerWithDependencyTypeConcreteTypeAndScopeMethod;

		public DependencyRegistry(
			INamedTypeSymbol objectScopeType,
			INamedTypeSymbol dependencyRegistryType,
			int objectScopeSingletonEnumValue
		) {
			m_objectScopeType = objectScopeType;
			m_dependencyRegistryType = dependencyRegistryType;
			m_objectScopeSingletonEnumValue = objectScopeSingletonEnumValue;

			var methods = m_dependencyRegistryType.GetMembers().OfType<IMethodSymbol>();
			foreach( var method in methods ) {

				// void Register<TDependencyType>( TDependencyType instance );
				if( method.Name == "Register" && method.IsGenericMethod && method.TypeParameters.Length == 1 && method.Parameters.Length == 1 ) {
					m_registerConcreteObjectMethod = method;
				}

				// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
				else if( method.Name == "Register" && method.IsGenericMethod && method.TypeParameters.Length == 2 && method.Parameters.Length == 1 ) {
					m_registerWithDependencyTypeAndConcreteTypeMethod = method;

				}

				// void Register( Type dependencyType, Type concreteType, ObjectScope scope );
				else if( method.Name == "Register" && !method.IsGenericMethod && method.Parameters.Length == 3 ) {
					m_registerWithDependencyTypeConcreteTypeAndScopeMethod = method;
				}
			}
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
			var singletonScopeEnumField = objectScopeType.GetMembers().FirstOrDefault( m => m.Name == "Singleton" ) as IFieldSymbol;
			if( singletonScopeEnumField == null || !singletonScopeEnumField.HasConstantValue ) {
				registry = null;
				return false;
			}
			var singletonScopeValue = (int)singletonScopeEnumField.ConstantValue;

			registry = new DependencyRegistry( 
				dependencyRegistryType: dependencyRegistryType, 
				objectScopeType: objectScopeType,
				objectScopeSingletonEnumValue: singletonScopeValue
			);
			return true;
		}

		/// <summary>
		/// Trys to get the register method represented by the expression. 
		/// Only non-factory, non-plugin methods are supported.
		/// </summary>
		public bool TryGetSingletonRegistration( 
			InvocationExpressionSyntax expression, 
			SemanticModel semanticModel, 
			out IMethodSymbol method,
			out SeparatedSyntaxList<ArgumentSyntax> arguments 
		) {
			method = semanticModel.GetSymbolInfo( expression ).Symbol as IMethodSymbol;
			arguments = default( SeparatedSyntaxList<ArgumentSyntax> );

			if( method == null ) {
				return false;
			}

			var evenMoreGenericMethod = method.ConstructedFrom;
			var isOneOfTheMethods = evenMoreGenericMethod == m_registerConcreteObjectMethod
				|| evenMoreGenericMethod == m_registerWithDependencyTypeAndConcreteTypeMethod
				|| evenMoreGenericMethod == m_registerWithDependencyTypeConcreteTypeAndScopeMethod;
			if( !isOneOfTheMethods) {
				return false;
			}

			if( expression.ArgumentList == null || expression.ArgumentList.Arguments.Count == 0 ) {
				return false;
			}
			arguments = expression.ArgumentList.Arguments;

			// there is either an ObjectScope parameter, it is always the last one, and it must be "Singleton"
			// or they're registering an actual object, which is always a singleton
			var scopeParameter = method.Parameters.LastOrDefault();
			var scopeArgument = arguments.LastOrDefault();
			if( scopeParameter != null && scopeParameter.Type == m_objectScopeType ) {
				var scopeArgumentValue = semanticModel.GetConstantValue( scopeArgument.Expression );
				if( !scopeArgumentValue.HasValue ) {
					return false;
				}
				if( (int)scopeArgumentValue.Value != m_objectScopeSingletonEnumValue ) {
					return false;
				}
			}

			return true;
		}

		public ITypeSymbol GetConcreteTypeFromSingletonRegistration( 
			SemanticModel semanticModel, 
			IMethodSymbol registerMethod, 
			SeparatedSyntaxList<ArgumentSyntax> methodArguments
		) {
			var evenMoreGenericMethod = registerMethod.ConstructedFrom;

			if( evenMoreGenericMethod == m_registerConcreteObjectMethod ) {
				// void Register<TDependencyType>( TDependencyType instance );
				var argument = methodArguments.First();
				var argumentsType = semanticModel.GetTypeInfo( argument.Expression ).Type;
				return argumentsType;

			} else if( evenMoreGenericMethod == m_registerWithDependencyTypeAndConcreteTypeMethod ) {
				// void Register<TDependencyType, TConcreteType>( ObjectScope scope )
				return registerMethod.TypeArguments.Skip( 1 ).FirstOrDefault();

			} else if( evenMoreGenericMethod == m_registerWithDependencyTypeConcreteTypeAndScopeMethod ) {
				// void Register( Type dependencyType, Type concreteType, ObjectScope scope );

				// This only handles the case where `typeof` is used inline.
				// This sucks, but there are very few usages of this method (it's
				// only used in open-generic types), so we don't care.
				//		r.Register( typeof(IFoo), typeof(Foo), ObjectScope.Singleton );
				var arg = methodArguments.Skip( 1 ).FirstOrDefault();
				var typeofExpression = arg?.Expression as TypeOfExpressionSyntax;
				if( typeofExpression == null ) {
					return null;
				}
				var argumentValue = semanticModel.GetSymbolInfo( typeofExpression.Type ).Symbol;
				return argumentValue as ITypeSymbol;
			}

			throw new NotSupportedException( "unsupported registration method" );
		}
	}


}
