// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutableGenericArgumentAnalyzer

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute {
			public Except Except { get; set; }
		}
		public sealed class Immutable : ImmutableAttributeBase { }
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }
	}
}

namespace SpecTests {
	using D2L.CodeStyle.Annotations;

	class ImmutableTypeArgumentTests {

		[Objects.Immutable]
		interface BaseInterface<[Objects.Immutable] T> {
		}

		[Objects.ImmutableBaseClass]
		class BaseClass<[Objects.Immutable] T> {
		}

		class MutableBaseClass<T> {
		}

		[Objects.Immutable]
		class ImplementedWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseInterface<T> {
		}

		[Objects.Immutable]
		class ImplementedWithImmutable<[Objects.Immutable] T> : BaseInterface<T> {
		}

		[Objects.Immutable]
		class DescendantWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseClass<T> {
		}

		[Objects.Immutable]
		class DescendantWithImmutable<[Objects.Immutable] T> : BaseClass<T> {
		}

		[Objects.Immutable]
		class ConstrainedWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> where T : BaseClass<T> {
		}

		[Objects.Immutable]
		class ConstrainedWithImmutable<[Objects.Immutable] T> where T : BaseClass<T> {
		}

		interface SubInterfaceWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseInterface<T> {
		}

		interface SubInterfaceWithImmutable< [Objects.Immutable] T> : BaseInterface<T> {
		}

		struct StructWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseInterface<T> {
		}

		struct StructWithImmutable< [Objects.Immutable] T> : BaseInterface<T> {
		}

		class MixedDescendantWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : MutableBaseClass<T>, BaseInterface<T> {
		}

		class MixedDescendantWithImmutable<[Objects.Immutable] T> : MutableBaseClass<T>, BaseInterface<T> {
		}

		class MixedConstrainedWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseInterface<T> where T: MutableBaseClass<T> {
		}

		class MixedConstrainedWithImmutable< [Objects.Immutable] T >: BaseInterface<T> where T : MutableBaseClass<T> {
		}
	}

	class MultipleArgumentTests {
		interface ImmutableInterface< [Objects.Immutable] T> {
		}

		class ImmutableBase< [Objects.Immutable] T > {
		}

		interface MutableInterface<T> {
		}

		class MutableBase<T> {
		}

		class ImmutableInterfaceMutableBase< /* GenericArgumentImmutableMustBeApplied */ S /**/, T>: MutableBase<T>, ImmutableInterface<S> {
		}

		class ImmutableInterfaceMutableBaseWithImmutable< [Objects.Immutable] S, T> : MutableBase<T>, ImmutableInterface<S> {
		}

		class MutableInterfaceImmutableBase< S, /* GenericArgumentImmutableMustBeApplied */ T /**/> : ImmutableBase<T>, MutableInterface<S> {
		}

		class MutableInterfaceImmutableBaseWithImmutable< S, [Objects.Immutable] T > : ImmutableBase<T>, MutableInterface<S> {
		}

		class MutableInterfaceImmutableConstraint< /* GenericArgumentImmutableMustBeApplied */ S /**/, T>: MutableInterface<S> where S: ImmutableBase<S> {
		}

		class MutableInterfaceImmutableConstraintWithImmutable< [Objects.Immutable] S, T> : MutableInterface<T> where S : ImmutableBase<S> {
		}

		class ImmutableInterfaceMutableConstraint< /* GenericArgumentImmutableMustBeApplied */ S /**/, T>: ImmutableInterface<S> where T: MutableBase<T> {
		}

		class ImmutableInterfaceMutableConstraintWithImmutable< [Objects.Immutable] S, T> : ImmutableInterface<S> where T : MutableBase<T> {
		}

		struct StructBothInterfaces< T, /* GenericArgumentImmutableMustBeApplied */ S /**/>: ImmutableInterface<S>, MutableInterface<T> {
		}

		struct StructBothInterfacesWithImmutable<T, [Objects.Immutable] S> : ImmutableInterface<S>, MutableInterface<T> {
		}

		class SameArgumentBothInterfaces< /* GenericArgumentImmutableMustBeApplied */ S /**/> : MutableInterface<S>, ImmutableInterface<S> {
		}

		class SameArgumentBothInterfacesWithImmutable< [Objects.Immutable] S> : MutableInterface<S>, ImmutableInterface<S> {
		}
	}

	class ConfirmationTests {
		// Just to ensure that the rule isn't triggering when it's not supposed to

		interface MutableBaseInterface<T> {
		}

		struct MutableStruct<T> : MutableBaseInterface<T> {
		}

		struct MutableStruct<[Objects.Immutable] T> : MutableBaseInterface<T> {
		}

		class MutableBase<T> {
		}

		class MutableClass<T>: MutableBase<T> {
		}

		class DescendantClass<[Objects.Immutable] T>: MutableBase<T> {
		}

		class ConstrainedClass<[Objects.Immutable] T> where T : MutableBase<T> {
		}

		class MutableConstrainedClass<T> where T: MutableBase<T> {
		}
	}
}