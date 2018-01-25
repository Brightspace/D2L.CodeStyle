// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.UnsafeSingletonsAnalyzer

using System;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
}

namespace D2L.CodeStyle.Annotations {
	public class Objects {
		public class Immutable : Attribute { }
	}
}

namespace SpecTests {

	[Singleton]
	interface ISingleton { }

	[Objects.Immutable]
	internal class SafeSingleton : ISingleton {
		private readonly string m_state;
	}

	internal class /* SingletonIsntImmutable() */ UnsafeSingleton /**/ : ISingleton {
		private readonly string m_state;
	}

	[Objects.Immutable]
	internal class ClearlyMutableImmutableButNotSingletonRaisesNoErrors {
		private string m_state;
	}

}
