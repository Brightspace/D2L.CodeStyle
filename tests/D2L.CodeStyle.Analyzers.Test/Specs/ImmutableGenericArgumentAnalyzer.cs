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

		[Objects.Immutable]
		class ImplementedWithoutImmutable< /* GenericArgumentImmutableMustBeApplied */ T /**/> : BaseInterface<T> {
		}

		[Objects.Immutable]
		class ImplementedWithImmutable<[Objects.Immutable] T> : BaseInterface<T> {
		}

		[Objects.ImmutableBaseClass]
		class BaseClass<[Objects.Immutable] T> {
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
	}
}