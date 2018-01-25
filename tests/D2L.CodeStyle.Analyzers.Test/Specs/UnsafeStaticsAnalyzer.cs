// analyzer: D2L.CodeStyle.Analyzers.Immutability.UnsafeStaticsAnalyzer

using System;
using D2L.CodeStyle.Analyzers.UnsafeStatics;
using D2L.CodeStyle.Annotations;
namespace D2L.CodeStyle.Annotations {
	public class Statics {
		public class Audited : Attribute { }
		public class Unaudited : Attribute { }
	}
	public class Objects {
		public class Immutable : Attribute { }
	}
}

namespace SpecTests {
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

		[Objects.Immutable]
		private class NotImmutableButBlessed {
			public int changeMe;
		}

		// The [Objects.Immutable] attribute is a hammer (but your field still must be readonly)
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

		public class CoRecursiveTypeA : CoRecursiveTypeB { }
		public class CoRecursiveTypeB : CoRecursiveTypeA { }

		// This should not crash our analyzer due to an infinite loop. It is
		// not valid code, though.
		private static readonly CoRecursiveTypeA m_recursiveA = new CoRecursiveTypeA();
		private static readonly CoRecursiveTypeB m_recursiveA = new CoRecursiveTypeB();

		public sealed class ThingWithUnknownBaseType : SomethingThatDoesntExist { }

		// The analyzer sees that SomethingTHatDoesntExist is IErrorType and
		// ignores it. That's safe because this code wouldn't compile anyway.
		private static readonly ThingWithUnknownBaseType m_unknownBaseType;

		public class OkBaseClass {
			private readonly int m_x = 0;
		}

		public sealed class OkClassWithBase : OkBaseClass { }

		private static readonly OkClassWithBase m_okWithBase = new OkClassWithBase();

		private class ConcretelySafeIfYouLookAtInitializers {
			private static OkClassWithBase Foo() { return null; }
			private readonly OkBaseClass m_ok1 = new OkBaseClass();
			private readonly OkBaseClass m_ok2 = Foo();
			private readonly OkBaseClass m_ok3 = null;
		}

		private static readonly ConcretelySafeIfYouLookAtInitializers m_concretelySafeIfYouLookAtInitializers
			= new ConcretelySafeIfYouLookAtInitializers();

		private class ClassWithTypelessInitializerExpression {
			private readonly int[] m_typelessExpression = { 1, 2, 3 };
		}

		[Statics.Unaudited( Because.ItsStickyDataOhNooo )]
		private static readonly ClassWithTypelessInitializerExpression m_unsafeClassWithTypelessInitializerExpression
			= new ClassWithTypelessInitializerExpression();

		// Tuple's are a blessed "container" type
		private static readonly Tuple<int, string> m_aTuple;
	}

	public class MutableBaseClass {
		private int m_mutableInt;
	}

	public sealed class ClassWithMutableBaseClass : MutableBaseClass {}

	public sealed class UnsafeThings {
		private static int /* UnsafeStatic(m_mutableInt,'m_mutableInt' is not read-only) */ m_mutableInt /**/;

		private static readonly ClassWithMutableBaseClass /* UnsafeStatic(m_foo,'m_foo.m_mutableInt' is not read-only) */ m_foo /**/;

		private class UnsafeThingWithInitializer {
			private readonly MutableBaseClass m_eh = new MutableBaseClass();
		}

		private static readonly UnsafeThingWithInitializer /* UnsafeStatic(m_unsafeWithInit,'m_unsafeWithInit.m_eh.m_mutableInt' is not read-only) */ m_unsafeWithInit = new UnsafeThingWithInitializer() /**/;
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
