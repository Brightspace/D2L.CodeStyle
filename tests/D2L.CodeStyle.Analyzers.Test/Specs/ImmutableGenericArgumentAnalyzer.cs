// analyzer: D2L.CodeStyle.Analyzers.Language.ImmutableGenericArgumentAnalyzer

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