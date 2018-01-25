using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	internal sealed class DependencyRegistry {
		private static readonly ImmutableArray<DependencyRegistrationExpression> s_registrationExpressions = ImmutableArray.Create<DependencyRegistrationExpression>(
			new RegisterParentAwareFactoryExpression(),
			new RegisterInstantiatedObjectExpression(),
			new NonGenericRegisterExpression(),
			new FullyGenericRegisterExpression(),
			new RegisterSubInterfaceExpression(),
			new ConfigurePluginsExpression(),
			new ConfigureInstancePluginsExpression(),
			new RegisterDynamicObjectFactoryExpression(),
			new RegisterPluginForExtensionPointExpression(),
			new RegisterExtensionPointExpression()
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
		public bool TryMapRegistrationMethod(
			IMethodSymbol method,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			SemanticModel semanticModel,
			out DependencyRegistrationExpression mappedRegistrationExpression
		) {
			mappedRegistrationExpression = s_registrationExpressions.FirstOrDefault(
				expr => expr.CanHandleMethod( method )
			);
			return mappedRegistrationExpression != null;
		}

		public bool IsRegistationMethod( IMethodSymbol method ) {
			if( method.ContainingType != m_dependencyRegistryType && method.ReceiverType != m_dependencyRegistryType ) {
				return false;
			}

			// if we have a handler for it, then yes
			var mappedHandler = s_registrationExpressions.FirstOrDefault(
				handler => handler.CanHandleMethod( method )
			);
			if( mappedHandler != null ) {
				return true;
			}

			// otherwise, if it's a method on IDependencyRegistry, then yes
			return method.ContainingType == m_dependencyRegistryType;
		}
	}
}
