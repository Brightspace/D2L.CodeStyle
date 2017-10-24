// analyzer: D2L.CodeStyle.Analyzers.UnsafeSingletons.UnsafeSingletonsAnalyzer

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
	public class Singletons {
		public class AuditedAttribute : Attribute { }
		public class UnauditedAttribute : Attribute { }
	}
}

namespace SpecTests {

	[Singleton]
	interface ISingleton { }

	internal class SafeSingleton : ISingleton {
		private readonly string m_state;
	}

	internal class /* SingletonIsntImmutable('m_state' is not read-only) */ UnsafeSingleton /**/ : ISingleton {
		private string m_state;
	}

	[Singletons.Audited]
	internal class UnsafeSingletonButAudited : ISingleton {
		private string m_state;
	}

	[Singletons.Unaudited]
	internal class UnsafeSingletonButUnaudited : ISingleton {
		private string m_state;
	}

	[Singletons.Audited]
	internal class /* UnnecessarySingletonAnnotation(Singletons.Audited,SpecTests.SafeSingletonButErroneouslyAudited) */ SafeSingletonButErroneouslyAudited /**/ : ISingleton {
		private readonly string m_state;
	}

	[Singletons.Unaudited]
	internal class /* UnnecessarySingletonAnnotation(Singletons.Unaudited,SpecTests.SafeSingletonButErroneouslyUnaudited) */ SafeSingletonButErroneouslyUnaudited /**/ : ISingleton {
		private readonly string m_state;
	}

	[Singletons.Audited]
	[Singletons.Unaudited]
	internal class /* ConflictingSingletonAnnotation() */ UnsafeSingletonWithConflictAnnotations /**/ : ISingleton {
		private string m_state;
	}

	[Objects.Immutable]
	internal class ClearlyMutableImmutableButNotSingletonRaisesNoErrors {
		private string m_state;
	}

}
