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
	public static class Mutability {
		public sealed class AuditedAttribute : Attribute { }
		public sealed class UnauditedAttribute : Attribute { }
	}
}

namespace SpecTests {

	class AnnotationsTests {
		[Objects.Immutable]
		interface IImmutable { }

		class /* ImmutableClassIsnt('m_bad' is not read-only) */ ClassWithMutableStateFails /**/ : IImmutable {
			private int m_bad;
		}

		class ClassWithAnnotatedMutableStateDoesntFail : IImmutable {
			[Mutability.Audited]
			private int m_unauditedBad;
			[Mutability.Unaudited]
			private int m_auditedBad;
		}
	}

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

		[Objects.Immutable]
		class IndexerPropertyClass {
			object this[ int index ] {
				get { return null; }
			}
		}

		#region Infinite recursion with type parameters in the loop
		[Objects.Immutable]
		interface IGenericInterface<T> {
			T Tee { get; }
		}

		[Objects.Immutable]
		class ClassWhichHoldsARecursiveGenericType {
			readonly IGenericInterface<ClassWhichHoldsARecursiveGenericType> r;
		}
		#endregion

		#region Type parameter new initializer
		[Objects.Immutable]
		public sealed class /* ImmutableClassIsnt('m_t''s type ('T') is a generic type) */ GenericWithFieldInitializer<T> /**/ {
			private readonly T m_t = new T();
		}

		#endregion

		#region Structs get checked for immutability
		[Objects.Immutable]
		struct /* ImmutableClassIsnt('x' is not read-only) */ Foo /**/ {
			int x;
		}
		#endregion

		#region No exponential worst case (thanks cache)

		[Objects.Immutable] // there are 3^25 = 847,288,609,443 paths to check here :)
		sealed class A { readonly B b1, b2, b3; }

		sealed class B { readonly C c1, c2, c3; }
		sealed class C { readonly D d1, d2, d3; }
		sealed class D { readonly E e1, e2, e3; }
		sealed class E { readonly F f1, f2, f3; }
		sealed class F { readonly G g1, g2, g3; }
		sealed class G { readonly H h1, h2, h3; }
		sealed class H { readonly I i1, i2, i3; }
		sealed class I { readonly J j1, j2, j3; }
		sealed class J { readonly K k1, k2, k3; }
		sealed class K { readonly L l1, l2, l3; }
		sealed class L { readonly M m1, m2, m3; }
		sealed class M { readonly N n1, n2, n3; }
		sealed class N { readonly O o1, o2, o3; }
		sealed class O { readonly P p1, p2, p3; }
		sealed class P { readonly Q q1, q2, q3; }
		sealed class Q { readonly R r1, r2, r3; }
		sealed class R { readonly S s1, s2, s3; }
		sealed class S { readonly T t1, t2, t3; }
		sealed class T { readonly U u1, u2, u3; }
		sealed class U { readonly V v1, v2, v3; }
		sealed class V { readonly W w1, w2, w3; }
		sealed class W { readonly X x1, x2, x3; }
		sealed class X { readonly Y y1, y2, y3; }
		sealed class Y { readonly Z z1, z2, z3; }
		sealed class Z {}

		#endregion
	}
}
