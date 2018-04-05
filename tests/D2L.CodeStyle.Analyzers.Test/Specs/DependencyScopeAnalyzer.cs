// analyzer: D2L.CodeStyle.Analyzers.DependencyScope.DependencyScopeAnalyzer

using System;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
	public sealed class WebRequestAttribute : Attribute { }
}

namespace SpecTests {

	struct SafeStruct {
	}

	[WebRequest]
	struct UnsafeStruct {
	}

	class SafeClass {
	}

	[WebRequest]
	class UnsafeClass {
	}

	[WebRequest]
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

	internal class EmptySingleton : ISingleton {
	}

	internal class SafeSingleton : ISingleton {
		private readonly SafeStruct m_struct;
	}

	internal class /* SingletonDependencyIsWebRequest() */ UnsafeStructSingleton /**/ : ISingleton {
		private readonly UnsafeStruct m_struct;
	}

	internal class /* SingletonDependencyIsWebRequest() */ UnsafeInterfaceSingleton /**/ : ISingleton {
		private readonly UnsafeInterface m_interface;
	}

	internal class /* SingletonDependencyIsWebRequest() */ UnsafeInterfaceSingleton /**/ : ISingleton {
		public UnsafeClass Property { get; }
	}

	internal class /* SingletonDependencyIsWebRequest() */ UnsafeClassSingleton /**/ : ISingleton {
		private readonly UnsafeClass m_struct;
	}

	internal class /* SingletonDependencyIsWebRequest() */ NestedUnsafeStructSingleton /**/ : ISingleton {
		private readonly NonSingletonUnsafeStructClass m_class;
	}

	internal class /* SingletonDependencyIsWebRequest() */ NestedUnsafeClassSingleton /**/ : ISingleton {
		private readonly NonSingletonUnsafeClassClass m_class;
	}

	internal class /* SingletonDependencyIsWebRequest() */ NestedUnsafeInterfaceSingleton /**/ : ISingleton {
		private readonly NonSingletonUnsafeInterfaceClass m_class;
	}

	internal class /* SingletonDependencyIsWebRequest() */ LazyUnsafeStructSingleton /**/ : ISingleton {
		private readonly Lazy<UnsafeStruct> m_struct => new Lazy<UnsafeStruct>(() => { return new UnsafeStruct(); });
	}

	internal class LazySafeStructSingleton : ISingleton {
		private readonly Lazy<SafeStruct> m_struct => new Lazy<SafeStruct>( () => { return new SafeStruct(); } );
	}
}
