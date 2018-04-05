// analyzer: D2L.CodeStyle.Analyzers.CustomerState.CustomerStateAnalyzer

using System;
using D2L.CodeStyle.Annotations.Attributes;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
}

namespace D2L.CodeStyle.Annotations.Attributes {
	public sealed class CustomerStateAttribute : Attribute { }
}

namespace SpecTests {

	struct SafeStruct {
	}

	[CustomerState]
	struct UnsafeStruct {
	}

	class SafeClass {
	}

	[CustomerState]
	class UnsafeClass {
	}

	[CustomerState]
	interface UnsafeInterface {

	}

	internal class NonSingletonUnsafeStructClass {
		private readonly UnsafeStruct m_struct;
	}

	internal class NonSingletonUnsafeClassClass {
		private readonly UnsafeClass m_class;
	}

	internal class NonSingletonUnsafeInterfaceClass {
		private readonly UnsafeInterface m_interface;
	}

	[Singleton]
	interface ISingleton { }

	interface Container<T> { }

	[CustomerState]
	internal class UnsafeContainer<T> {
	}

	internal class EmptySingleton : ISingleton {
	}

	internal class SafeSingleton : ISingleton {
		public readonly SafeStruct m_struct;
	}

	internal class /* SingletonDependencyHasCustomerState() */ UnsafeStructSingleton /**/ : ISingleton {
		public readonly UnsafeStruct m_struct;
	}

	internal class /* SingletonDependencyHasCustomerState() */ UnsafeInterfaceSingleton /**/ : ISingleton {
		public readonly UnsafeInterface m_interface;
	}

	internal class /* SingletonDependencyHasCustomerState() */ UnsafeInterfaceSingleton /**/ : ISingleton {
		public UnsafeClass Property { get; }
	}

	internal class /* SingletonDependencyHasCustomerState() */ UnsafeClassSingleton /**/ : ISingleton {
		public readonly UnsafeClass m_struct;
	}

	internal class /* SingletonDependencyHasCustomerState() */ NestedUnsafeStructSingleton /**/ : ISingleton {
		public readonly NonSingletonUnsafeStructClass m_class;
	}

	internal class /* SingletonDependencyHasCustomerState() */ NestedUnsafeClassSingleton /**/ : ISingleton {
		public readonly NonSingletonUnsafeClassClass m_class;
	}

	internal class /* SingletonDependencyHasCustomerState() */ NestedUnsafeInterfaceSingleton /**/ : ISingleton {
		public readonly NonSingletonUnsafeInterfaceClass m_class;
	}

	internal class /* SingletonDependencyHasCustomerState() */ LazyUnsafeStructSingleton /**/ : ISingleton {
		public readonly Lazy<UnsafeStruct> m_struct => new Lazy<UnsafeStruct>( () => { return new UnsafeStruct(); } );
	}

	internal class LazySafeStructSingleton : ISingleton {
		public readonly Lazy<SafeStruct> m_struct => new Lazy<SafeStruct>( () => { return new SafeStruct(); } );
	}

	internal class /* SingletonDependencyHasCustomerState() */ UnsafeContainerSingleton /**/ : ISingleton {
		public readonly UnsafeContainer<SafeStruct> m_class;
	}

	public class /* PublicClassHasHiddenCustomerState() */ ShouldHaveAttribute /**/ {
		private UnsafeClass m_class;
	}
}
