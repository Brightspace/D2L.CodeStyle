// analyzer: D2L.CodeStyle.Analyzers.UnsafeStatics.UnsafeStaticsAnalyzer

using System;

namespace SpecTests {
	public static class Statics {
		public sealed class UnauditedAttribute : Attribute { }
	}
	public class SafeThings {
		// Non-static members should not raise diagnostics.
		DateTime now = DateTime.Now;
		DateTime someTime { get; set; }

		// If a type can't be resolved our analyzer shouldn't crash.
		UnknownType weird;

		sealed class ImmutableClassWithInstanceVar {
			public static readonly ImmutableClassWithInstanceVar Instance = new ImmutableClassWithInstanceVar();
		}

		// This case is recursive: instanceHandle is safe only if
		// ImmutableClassWithInstanceVar is immutable. That class is immutable
		// only if all its members are. It's only member is of type
		// ImmutableClassWithInstanceVar.
		static readonly ImmutableClassWithInstanceVar instanceHandle = ImmutableClassWithInstanceVar.Instance;

		// DateTime is a value type, so it is immutable if its readonly. This is a
		// quirk in the C# language. For more information, see
		// https://codeblog.jonskeet.uk/2014/07/16/micro-optimization-the-surprising-inefficiency-of-readonly-fields/
		static readonly DateTime xyz = DateTime.Now;

		internal class TotallyImmutableType {}

		public static readonly TotallyImmutableType noBrainer = new TotallyImmutableType();

		internal class NonSealedButOtherwiseImmutableType {
			public readonly string yolo = "swag";
		}

		// Normally we are cautious about calling non-sealed types immutable.
		// This is because a derived class could introduce mutability. However,
		// we have a special-case optimization for readonly fields that have an
		// initializer of the form "new T()" - in that case we know the field
		// has a maximum type of T, so we can ignore the lack of sealed.
		public static readonly NonSealedButOtherwiseImmutableType foo = new NonSealedButOtherwiseImmutableType();

		// Interfaces are similar to non-sealed classes when it comes to the
		// new T() initializer exception.
		interface INotNecessarilyImmutableInterface { }
		sealed class ImmutableImplementation : INotNecessarilyImmutableInterface { }
		INotNecessarilyImmutableInterface blahBlah = new ImmutableImplementation();

		[Immutable]
		private class NotImmutableButBlessed {
			public int changeMe;
		}

		// The [Immutable] attribute is a hammer (but your field still must be readonly)
		public static readonly NotImmutableButBlessed urg = new NotImmutableButBlessed();

		// The Statics.Unaudited annotation supresses errors
		[Statics.Unaudited( Because.ItsStickyDataOhNooo )]
		public static int x;
		[Statics.Unaudited( Because.ItsStickyDataOhNooo )]
		public static readonly INotNecessarilyImmutableInterface x;

		internal sealed class ClassWithMemberOfUnknownType {
			private readonly INotSureWhatThisIs m_foo = null;
		}

		// This shouldn't emit a diagnostic and shouldn't throw an exception
		// even though we can't complete the analysis. That's okay because
		// our analyzer only needs to be strict for builds that pass.
		public static readonly ClassWithMemberOfUnknownType m_classWithMemberOfUnknownType;
	}

	public sealed class UnsafeThings {
		private static int /* UnsafeStatic(m_mutableInt,'m_mutableInt' is not read-only) */ m_mutableInt /**/;
	}

	public sealed class ValueTypeCases {

		// ValueType is the base class of all ValueTypes and it itself is safe
		//
		// Saquib points out that this shouldn't actually compile because
		// ValueType is abstract, but we're keeping it because its still an
		// OK test.
		private static readonly ValueType m_valueType = new ValueType();

		public struct UsuallyMutable {
			public int x;
		}

		// This gets marked as unsafe even though it technically isn't. Our
		// analyzer isn't smart enough to spot that.
		// https://codeblog.jonskeet.uk/2014/07/16/micro-optimization-the-surprising-inefficiency-of-readonly-fields/
		private static readonly ValueType /* UnsafeStatic(m_edgeCase,'m_edgeCase.x' is not read-only) */ m_edgeCase = new UsuallyMutable() /**/;

		public struct AlwaysMutable {
			public readonly string[] m_data;
		}

		// This case, however, should be marked unsafe because even with the
		// caveat above, the copy of the struct will have the same pointer to
		// m_data (a reference type) as the field itself.
		public static readonly ValueType /* UnsafeStatic(m_mutable,'m_mutable.m_data''s type ('System.String[]') is an array) */ m_mutable = new AlwaysMutable() /**/;

	}
}
