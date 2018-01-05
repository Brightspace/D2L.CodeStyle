using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	public sealed class DependencyRegistration {

		public ObjectScope ObjectScope { get; }

		public ITypeSymbol DependencyType { get; }

		public ITypeSymbol ConcreteType { get; }

		public ITypeSymbol FactoryType { get; }

		public ITypeSymbol DynamicObjectFactoryType { get; }

		private DependencyRegistration(
			ObjectScope scope,
			ITypeSymbol dependencyType,
			ITypeSymbol concreteType = null,
			ITypeSymbol factoryType = null,
			ITypeSymbol dynamicObjectType = null
		) {
			ObjectScope = scope;
			DependencyType = dependencyType;
			ConcreteType = concreteType;
			FactoryType = factoryType;
			DynamicObjectFactoryType = dynamicObjectType;
		}

		public static DependencyRegistration NonFactory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol concreteType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType,
				concreteType: concreteType
			);

		public static DependencyRegistration Factory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol factoryType, ITypeSymbol concreteType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType,
				factoryType: factoryType,
				concreteType: concreteType
			);

		public static DependencyRegistration DynamicObjectFactory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol dynamicObjectType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType,
				dynamicObjectType: dynamicObjectType
			);
	}

	/// <summary>
	/// This is copied from lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
	/// so that the values match.
	/// </summary>
	public enum ObjectScope {
		AlwaysCreateNewInstance = 0,
		Singleton = 1,
		Thread = 2,
		WebRequest = 3
	}

}
