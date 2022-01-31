using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	internal sealed class DependencyRegistration {
		private DependencyRegistration(
			ObjectScope scope,
			ITypeSymbol dependencyType,
			ITypeSymbol? concreteType = null,
			ITypeSymbol? factoryType = null
		) {
			ObjectScope = scope;
			DependencyType = dependencyType;
			ConcreteType = concreteType;
			FactoryType = factoryType;
		}

		public ObjectScope ObjectScope { get; }
		public ITypeSymbol DependencyType { get; }
		public ITypeSymbol? ConcreteType { get; }
		public ITypeSymbol? FactoryType { get; }

		internal static DependencyRegistration Marker( ObjectScope scope, ITypeSymbol dependencyType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType
			);

		internal static DependencyRegistration NonFactory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol concreteType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType,
				concreteType: concreteType
			);

		internal static DependencyRegistration Factory( ObjectScope scope, ITypeSymbol dependencyType, ITypeSymbol factoryType, ITypeSymbol concreteType )
			=> new DependencyRegistration(
				scope,
				dependencyType: dependencyType,
				factoryType: factoryType,
				concreteType: concreteType
			);
	}

	/// <summary>
	/// This is copied from lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
	/// so that the values match.
	/// </summary>
	internal enum ObjectScope {
		Singleton = 1,
		WebRequest = 3
	}

}
