// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
}

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public sealed class Immutable : Attribute { }
	}
}

namespace SpecTests {

	class GenericsTests {

		[Objects.Immutable]
		interface IImmutable { }

		interface IGenericImmutable<T> : IImmutable { }

		interface IGenericImmutableWithTypeConstraint<T> : IImmutable where T : IImmutable { }

		class GenericClassWithoutStateIsSafe<T> : IGenericImmutable<T> { }

		class /* ImmutableClassIsnt('foo''s type ('T') is a generic type) */ GenericClassWithStateIsUnsafe<T> /**/ : IGenericImmutable<T> {
			internal readonly T foo;
		}

		// todo: we should try and map generic parameters with arguments on implemented interfaces
		// and extract any constraints that we can
		class /* ImmutableClassIsnt('foo''s type ('T') is a generic type) */ IndirectlyConstrainedGenericClassWithStateIsUnsafe<T> /**/ : IGenericImmutableWithTypeConstraint<T> {
			internal readonly T foo;
		}

		class DirectlyConstrainedGenericClassWithStateIsSafe<T> : IGenericImmutable<T> where T : IImmutable {
			internal readonly T foo;
		}

	}
}
