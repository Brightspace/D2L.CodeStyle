﻿// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

[assembly: Objects.ImmutableGeneric(
	type: typeof( SpecTests.GenericsTests.IFactory<Version> )
)]

[assembly: Objects.ImmutableGeneric(
	type: typeof( IComparable<SpecTests.GenericsTests.ILocallyDefined> )
)]

namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
}

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute {
			public Except Except { get; set; }
		}
		public sealed class Immutable : ImmutableAttributeBase { }
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }

		[AttributeUsage( validOn: AttributeTargets.Assembly )]
		public sealed class ImmutableGenericAttribute : Attribute {
			public ImmutableGenericAttribute( Type type ) { }
		}

		[Flags]
		public enum Except {
			None = 0,
			ItHasntBeenLookedAt = 1,
			ItsSketchy = 2,
			ItsStickyDataOhNooo = 4,
			WeNeedToMakeTheAnalyzerConsiderThisSafe = 8,
			ItsUgly = 16,
			ItsOnDeathRow = 32
		}
	}
	public static class Mutability {
		public sealed class AuditedAttribute : Attribute { }
		public sealed class UnauditedAttribute : Attribute {
			public UnauditedAttribute( Because why ) { }
		}
	}
	public enum Because {
		ItHasntBeenLookedAt = 1,
		ItsSketchy = 2,
		ItsStickyDataOhNooo = 3,
		WeNeedToMakeTheAnalyzerConsiderThisSafe = 4,
		ItsUgly = 5,
		ItsOnDeathRow = 6
	}
}

namespace SpecTests {

	// Items used in multiple test spaces are defined outside of the spaces
	sealed class ImmutableClass {
		private readonly string m_ImmutableClass;
	}

	sealed class MutableClass {
		private string m_MutableClass;
	}



	class AnnotationsTests {
		[Objects.Immutable]
		interface IImmutable { }

		class /* ImmutableClassIsnt('m_bad' is not read-only) */ ClassWithMutableStateFails /**/ : IImmutable {
			private int m_bad;
		}

		class ClassWithAnnotatedMutableStateDoesntFail : IImmutable {
			[Mutability.Audited]
			private int m_unauditedBad;
			[Mutability.Unaudited( Because.ItsSketchy )]
			private int m_auditedBad;
		}
	}

	class ExternalMarkedImmutableTests {
		class /* ImmutableClassIsnt('m_bad' is not read-only) */ MutableEncoding /**/ : System.Text.Encoding {
			private int m_bad;
		}

		class AuditedMutableEncoding : System.Text.Encoding {
			[Mutability.Audited]
			private int m_bad;
		}

		class /* InvalidUnauditedReasonInImmutable(ItHasntBeenLookedAt) */ UnauditedMutableEncoding /**/ : System.Text.Encoding {
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			private int m_bad;
		}

		class ImmutableEncoding : System.Text.Encoding {
			private readonly int m_bad;
		}

		[Objects.Immutable]
		class EnclosedExternalImmutable {
			private readonly System.Text.Encoding m_encoding;
		}
	}

	class ImmutableBaseClassTests {

		[Objects.ImmutableBaseClass]
		class ImmutableBaseClass { }

		[Objects.Immutable]
		class ConcretelyImmutable { }

		[Objects.ImmutableBaseClass]
		class /* ImmutableClassIsnt('m_bad' is not read-only) */ MutableBaseClassOnly /**/ {
			private int m_bad;
		}

		class /* ImmutableClassIsnt('m_bad' is not read-only) */ ShouldBeRequiredImmutable /**/ : ConcretelyImmutable {
			private int m_bad;
		}
		class ShouldNotBeRequiredImmutable2 : ImmutableBaseClass {
			private int m_bad;
		}

		[Objects.Immutable]
		class /* ImmutableClassIsnt('m_bad''s type ('SpecTests.ImmutableBaseClassTests.ImmutableBaseClass') is not sealed) */ MutableBecauseHasFieldOfImmutableBaseClassOnly /**/ {
			private readonly ImmutableBaseClass m_bad;
		}

		[Objects.Immutable]
		class ImmutableBecauseHasFieldOfConcreteImmutableBaseClassOnly {
			private readonly ImmutableBaseClass m_bad = new ImmutableBaseClass();
		}

	}

	class GenericsTests {

		[Objects.Immutable]
		interface IImmutable { }

		interface IGenericImmutable<T> : IImmutable { }

		interface IGenericImmutableWithTypeConstraint<T> : IImmutable where T : IImmutable { }

		interface ILocallyDefined { }

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
			object this[int index] {
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

		#region Immutable generic types marked for certain `T`s
		public interface IFactory<T> { }

		[Objects.Immutable]
		public sealed class UsesMarkedImmutableTypeArg {
			private readonly IFactory<Version> m_safeFactory;
		}
		[Objects.Immutable]
		public sealed class /* ImmutableClassIsnt('m_unsafeFactory''s type ('SpecTests.GenericsTests.IFactory') is an interface that is not marked with `[Objects.Immutable]`) */ UsesUnsafeTypeArg /**/ {
			private readonly IFactory<string> m_unsafeFactory;
		}

		public sealed class GenericTypeHoldingGenericType<T> {
			private readonly IFactory<T> m_unsafeFactory;
		}

		[Objects.Immutable]
		public sealed class /* ImmutableClassIsnt('m_indirectThing.m_unsafeFactory''s type ('SpecTests.GenericsTests.IFactory') is an interface that is not marked with `[Objects.Immutable]`) */ OtherGenericClass<T> /**/ {
			// The string[] is important: when we go to look for an ImmutableGeneric for
			// that type parameter we bail out early because there is no containing
			// assembly for string[]. Switching GenericTypeHoldingGenericType here to
			// IFactory<string[]> doesn't exhibit the issue though, we need the
			// indirection... not sure exactly what's up.
			private readonly GenericTypeHoldingGenericType<string[]> m_indirectThing;
		}

		public sealed class /* ImmutableClassIsnt('bad' is not read-only) */ MutableButSubClassesImmutableGeneric /**/ : IFactory<Version> {
			private int bad;
		}

		public sealed class ImplementsImmutableGeneric : IComparable<ILocallyDefined> {
			private readonly IImmutable m_safeDependency;
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
		sealed class Z { }

		#endregion
	}

	class ImmutableExceptionTests {

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		sealed class /* InvalidUnauditedReasonInImmutable(ItsSketchy) */ ClassWithNotExceptedUnauditedReasonFails /**/ {
			[Mutability.Unaudited( Because.ItsSketchy )]
			private int m_auditedBad;
		}

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		sealed class ClassWithExceptedUnauditedReasonSucceeds {
			[Mutability.Unaudited( Because.ItsUgly )]
			private int m_auditedGood;
		}

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		class InheritedImmutable { }

		sealed class /* InvalidUnauditedReasonInImmutable(ItsSketchy) */ ClassWithInheritedNotExceptedUnauditedReasonFails /**/ : InheritedImmutable {
			[Mutability.Unaudited( Because.ItsSketchy )]
			private int m_auditedBad;
		}

		sealed class ClassWithInheritedExceptedUnauditedReasonSucceeds : InheritedImmutable {
			[Mutability.Unaudited( Because.ItsUgly )]
			private int m_auditedGood;
		}

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		interface IImplementedImmutable { }

		sealed class /* InvalidUnauditedReasonInImmutable(ItsSketchy) */ ClassWithImplementedNotExceptedUnauditedReasonFails /**/ : IImplementedImmutable {
			[Mutability.Unaudited( Because.ItsSketchy )]
			private int m_auditedBad;
		}

		sealed class ClassWithImplementedExceptedUnauditedReasonSucceeds : IImplementedImmutable {
			[Mutability.Unaudited( Because.ItsUgly )]
			private int m_auditedGood;
		}

		[Objects.Immutable( Except = Objects.Except.ItsSketchy )]
		interface IImmutableMember { }

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		sealed class /* InvalidUnauditedReasonInImmutable(ItsSketchy) */ ClassWithNotExceptedImmutableMemberFails /**/ {
			private readonly IImmutableMember m_auditedBad;
		}

		[Objects.Immutable( Except = Objects.Except.ItsSketchy )]
		sealed class ClassWithExceptedImmutableMemberSucceeds {
			private readonly IImmutableMember m_auditedBad;
		}

	}
}