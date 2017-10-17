using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.DependencyInjection {
	internal sealed class DependencyRegistration {

		public ObjectScope ObjectScope { get; }

		public ITypeSymbol DependencyType { get; }

		public ITypeSymbol ConcreteType { get; }

		public ITypeSymbol FactoryType { get; }

		public bool IsFactoryRegistration { get; }

		private DependencyRegistration(
			ObjectScope scope,
			ITypeSymbol dependencyType,
			ITypeSymbol concreteType,
			ITypeSymbol factoryType,
			bool isFactoryRegistration
		) {
			ObjectScope = scope;
			DependencyType = dependencyType;
			ConcreteType = concreteType;
			FactoryType = factoryType;
			IsFactoryRegistration = isFactoryRegistration;
		}

		internal static DependencyRegistration NonFactory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol concreteType )
			=> new DependencyRegistration( 
				scope, 
				dependencyType: dependencyType,
				concreteType: concreteType, 
				factoryType: null, 
				isFactoryRegistration: false 
			);

		internal static DependencyRegistration Factory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol factoryType )
			=> new DependencyRegistration( 
				scope,
				dependencyType: dependencyType,
				concreteType: null, 
				factoryType: factoryType, 
				isFactoryRegistration: true 
			);
	}

	/// <summary>
	/// This is copied from lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
	/// so that the values match.
	/// </summary>
	internal enum ObjectScope {
		AlwaysCreateNewInstance = 0,
		Singleton = 1,
		Thread = 2,
		WebRequest = 3
	}

}
