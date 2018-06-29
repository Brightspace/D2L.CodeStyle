// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

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
		private object m_MutableClass;
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

		class /* ImmutableClassIsnt('foo''s type ('T') is not deterministically immutable) */ GenericClassWithStateIsUnsafe<T> /**/ : IGenericImmutable<T> {
			internal readonly T foo;
		}

		// todo: we should try and map generic parameters with arguments on implemented interfaces
		// and extract any constraints that we can
		class /* ImmutableClassIsnt('foo''s type ('T') is not deterministically immutable) */ IndirectlyConstrainedGenericClassWithStateIsUnsafe<T> /**/ : IGenericImmutableWithTypeConstraint<T> {
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
		public sealed class /* ImmutableClassIsnt('m_t''s type ('T') is not deterministically immutable) */ GenericWithFieldInitializer<T> /**/ {
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

	class GenericTypeFieldTests {
		// Generic type parameter state with mutable and immutable concrete types

		class GenericClassWithNoState<[Objects.Immutable] T> {
			// Test to ensure that marking a type parameter and not holding it has no effect
		}

		class MutableGenericClassWithImmutableState<[Objects.Immutable] T> {
			// Test to ensure that marking a type parameter and holding it, but 
			// not being yourself immutable causes no problems
			internal T m_GenericClassWithImmutableState;
		}

		[Objects.Immutable]
		sealed class GenericClassWithImmutableState<[Objects.Immutable] T> {
			internal readonly T m_GenericClassWithImmutableState;
		}

		[Objects.Immutable]
		class /* ImmutableClassIsnt('m_ConcreteClassWithMutableGenericType.m_GenericClassWithImmutableState.m_MutableClass' is not read-only) */ ConcreteClassWithMutableGenericType /**/ {
			internal readonly GenericClassWithImmutableState<MutableClass> m_ConcreteClassWithMutableGenericType;
		}

		[Objects.Immutable]
		class ConcreteClassWithImmutableGenericType {
			internal readonly GenericClassWithImmutableState<ImmutableClass> m_ConcreteClassWithImmutableGenericType;
		}
	}

	class GenericTypeParameterTests {
		// Ensures generic type parameters from an interface are examined

		[Objects.Immutable]
		interface ImmutableGenericInterface<[Objects.Immutable] T> {
		}

		[Objects.Immutable]
		sealed class GenericClassWithImmutableInterface<T> : ImmutableGenericInterface<T> {
			internal readonly T m_GenericClassWithImmutableInterface;
		}

		[Objects.Immutable]
		class ConcreteClassWithImmutableInterfaceImmutableType {
			internal readonly ImmutableGenericInterface<ImmutableClass> m_ConcreteClassWithImmutableInterfaceImmutableType;
		}

		[Objects.Immutable]
		class /* ImmutableClassIsnt('m_ConcreteClassWithImmutableInterfaceMutableType''s type ('SpecTests.MutableClass') is a type parameter that must be marked with `[Objects.Immutable]`) */ ConcreteClassWithImmutableInterfaceMutableType /**/ {
			internal readonly ImmutableGenericInterface<MutableClass> m_ConcreteClassWithImmutableInterfaceMutableType;
		}

		[Objects.Immutable]
		class /* ImmutableClassIsnt('m_mutableClass.m_MutableClass' is not read-only) */ ConcreteImmutableFromInterface /**/ : ImmutableGenericInterface<MutableClass> {
			private readonly MutableClass m_mutableClass;
		}
	}

	class MultipleGenericParameterTests {
		// Tests when there is a mixed set of mutable and immutable type parameters

		[Objects.Immutable]
		sealed class MultiGenericClassWithOneImmutable<[Objects.Immutable] T, S> {
			internal readonly T m_MultiGenericClassWithOneImmutable;
		}

		[Objects.Immutable]
		sealed class MultiImmutable<[Objects.Immutable] S, [Objects.Immutable] T> {
			internal readonly S m_MultiGenericClassWithOneImmutableS;
			internal readonly T m_MultiGenericClassWithOneImmutableT;
		}

		[Objects.Immutable]
		sealed class MultiImmutableState {
			private readonly MultiImmutable<ImmutableClass, ImmutableClass> m_MultiImmutableState;
		}

		[Objects.Immutable]
		sealed class ConcreteHoldingMixedModeGeneric {
			internal readonly MultiGenericClassWithOneImmutable<ImmutableClass, MutableClass> m_ConcreteHoldingMixedModeGeneric;
		}

		[Objects.Immutable]
		sealed class /* ImmutableClassIsnt('m_ConcreteHoldingMixedModeGenericWrongOrder.m_MultiGenericClassWithOneImmutable.m_MutableClass' is not read-only) */ ConcreteHoldingMixedModeGenericWrongOrder /**/ {
			internal readonly MultiGenericClassWithOneImmutable<MutableClass, ImmutableClass> m_ConcreteHoldingMixedModeGenericWrongOrder;
		}
	}

	class MultipleGenericInterfaceParameterTests {
		[Objects.Immutable]
		interface MultiGeneric<[Objects.Immutable] S, T> {
		}

		sealed class MultiGenericFromInterface<S, T> : MultiGeneric<S, T> {
			internal readonly S m_MultiGenericFromInterface;
		}

		[Objects.Immutable]
		sealed class ConcreteUsingMultiGenericFromInterface {
			internal readonly MultiGenericFromInterface<ImmutableClass, NonReadOnlyClass> m_ConcreteUsingMultiGenericFromInterface;
		}

		[Objects.Immutable]
		sealed class /* ImmutableClassIsnt('m_ConcreteUsingMultiGenericFromInterfaceWrongOrder.m_MultiGenericFromInterface.m_MutableClass' is not read-only) */ ConcreteUsingMultiGenericFromInterfaceWrongOrder /**/ {
			internal readonly MultiGenericFromInterface<MutableClass, ImmutableClass> m_ConcreteUsingMultiGenericFromInterfaceWrongOrder;
		}

		[Objects.Immutable]
		interface MultiImmutable<[Objects.Immutable] S, [Objects.Immutable] T> {
		}

		[Objects.Immutable]
		sealed class MultiImmutableFromInterface<S, T> : MultiImmutable<S, T> {
			internal readonly S m_MultiGenericFromInterfaceS;
			internal readonly T m_MultiGenericFromInterfaceT;
		}

		[Objects.Immutable]
		sealed class MultiImmutableState {
			internal readonly MultiImmutableFromInterface<ImmutableClass, ImmutableClass> m_MultiImmutableState;
		}

	}

	class GenericBaseClassTests {
		// Generic type parameter state with mutable and immutable concrete types

		[Objects.ImmutableBaseClass]
		class ImmutableBase<[Objects.Immutable] T> {
			internal readonly T m_ImmutableBase;
		}

		[Objects.Immutable]
		sealed class /* ImmutableClassIsnt('m_MutableClass' is not read-only) */ MutableImpl /**/ : ImmutableBase<MutableClass> {
		}

		[Objects.Immutable]
		sealed class ImmutableImpl : ImmutableBase<ImmutableClass> {
		}
	}

	class BaseParameterInspectionTests {

		[Objects.Immutable]
		interface IImmutableRoot<[Objects.Immutable] W> {
		}

		interface IMiddle<T> : IImmutableRoot<T> {
		}

		interface IMutableRoot<T> {
		}

		[Objects.ImmutableBaseClass]
		class MixinBase<U, V> : IMutableRoot<U>, IMiddle<V> {
			private readonly V m_V;
		}

		class ImmutableConcrete : MixinBase<MutableClass, ImmutableClass> {
		}

		class /* ImmutableClassIsnt('m_MutableClass' is not read-only) */ MutableConcrete /**/ : MixinBase<ImmutableClass, MutableClass> {
		}
	}

	class GenericConstraintTests {

		[Objects.Immutable]
		public abstract class ImmutableConstraint {
			private readonly string m_ImmutableConstraint;
		}

		public abstract class MutableConstraint {
			private readonly object m_MutableConstraint;
		}

		[Objects.Immutable]
		public abstract class GenericImmutableConstraint<T> where T : ImmutableConstraint {

			public T m_GenericImmutableConstraint { get; }
		}

		[Objects.Immutable]
		public abstract class /* ImmutableClassIsnt('m_GenericMutableConstraint''s type ('T') is not deterministically immutable) */ GenericMutableConstraint<T> /**/ where T : MutableConstraint {

			public T m_GenericMutableConstraint { get; }
		}

	}

	class ImmutableBaseAndConstraintTests {
		// Test to set up and confirm various ways in which the code is actually
		// written will pass

		[Objects.Immutable]
		public abstract class ImmutableGenericConstraint<[Objects.Immutable] T> {

			public T Value { get; }
		}

		[Objects.Immutable]
		public interface IImmutableInterface {
		}

		// Gets Immutable from the interface, and the property U
		// gets its Immutable from the constraint
		internal class ImmutableGenericBase<U, V>
			: IImmutableInterface
			where U : ImmutableGenericConstraint<V> {

			private readonly U m_feature;
		}

		[Objects.Immutable]
		internal class ImmutableClassWithImmutableConstrainedState<T>
			where T : ImmutableGenericConstraint<T> {

			private readonly T m_ImmutableClass;
		}

		class ConcreteGeneric : ImmutableGenericBase<ImmutableGenericConstraint<ImmutableClass>, ImmutableClass> {
		}

		class /* ImmutableClassIsnt('Value.m_MutableClass' is not read-only) */ BadConcreteGeneric /**/: ImmutableGenericBase<ImmutableGenericConstraint<MutableClass>, MutableClass> {
		}

	}

	class AuditedBaseTests {
		[Objects.Immutable]
		public abstract class AuditedConstraint<TValue> {

			[Mutability.Audited(
				owner: "Test",
				auditedDate: "26-Jun-2018",
				rationale: "Testing audited in tests"
			)]
			public TValue Value { get; }
		}

		[Objects.Immutable]
		public interface IAuditedItem<TDefinition, TValue>
			where TDefinition : AuditedConstraint<TValue> {

			TValue GetValue();
		}

		internal sealed class Audited<TDefinition, TValue>
			: IAuditedItem<TDefinition, TValue>
			where TDefinition : AuditedConstraint<TValue> {

			private readonly TDefinition m_feature;
		}

		internal class ConcreteAudited : Audited<AuditedConstraint<ImmutableClass>, ImmutableClass> {
		}

		// This should still pass because the Audited attribute is saying it's
		// okay to pass it mutable types, they're all used in a safe manner.
		internal class MutableConcreteAudited : Audited<AuditedConstraint<MutableClass>, MutableClass> {
		}
	}

	class BaseGenericMixinTests {

		[Objects.ImmutableBaseClass]
		class BaseState<S, [Objects.Immutable] T> {
			private readonly T m_BaseState;
		}

		[Objects.Immutable]
		class WithState : BaseState<MutableClass, ImmutableClass> {
		}

		[Objects.Immutable]
		class /* ImmutableClassIsnt('m_MutableClass' is not read-only) */ WithBadState /**/ : BaseState<ImmutableClass, MutableClass> {
		}
	}
}