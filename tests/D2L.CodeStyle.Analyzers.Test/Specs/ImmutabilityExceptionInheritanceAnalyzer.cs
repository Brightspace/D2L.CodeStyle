// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityExceptionInheritanceAnalyzer

using System;
using D2L.CodeStyle.Annotations;

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public sealed class Immutable : Attribute {
			public Except Except { get; set; }
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
}


namespace SpecTests {

	class DefaultInheritance {

		[Objects.Immutable]
		interface IDefaultImmutableInterface { }

		sealed class ClassNotMarkedImmutableImplementingDefaultImmutable : IDefaultImmutableInterface { }

		[Objects.Immutable]
		sealed class ClassWithoutSpecifiedExceptionsImplementingDefaultImmutable : IDefaultImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt )]
		sealed class ClassWithSpecifiedExceptionsImplementingDefaultImmutable : IDefaultImmutableInterface { }

	}

	class SpecifiedExceptionInheritance {

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt )]
		interface IExceptedImmutableInterface { }

		sealed class ClassNotMarkedImmutableImplementingExceptedImmutable : IExceptedImmutableInterface { }

		[Objects.Immutable]
		sealed class /* ImmutableExceptionInheritanceIsInvalid(IExceptedImmutableInterface,Reduce the [Immutable] exceptions on this type to a subset of { ItHasntBeenLookedAt }.) */ ClassWithoutSpecifiedExceptionsImplementingExceptedImmutable /**/ : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt )]
		sealed class ClassWithSpecifiedExceptionsImplementingExceptedImmutableSame : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt )]
		sealed class ClassWithSpecifiedExceptionsImplementingExceptedImmutableSubset : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.ItsOnDeathRow )]
		sealed class /* ImmutableExceptionInheritanceIsInvalid(IExceptedImmutableInterface,Reduce the [Immutable] exceptions on this type to a subset of { ItHasntBeenLookedAt }.) */ ClassWithSpecifiedExceptionsImplementingExceptedImmutableNewException /**/ : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt | Objects.Except.ItsOnDeathRow )]
		interface /* ImmutableExceptionInheritanceIsInvalid(IExceptedImmutableInterface,Reduce the [Immutable] exceptions on this type to a subset of { ItHasntBeenLookedAt }.) */ InheritingSupersetOfExceptions /**/ : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.None )]
		interface InheritingSubsetOfExceptions : IExceptedImmutableInterface { }

		[Objects.Immutable( Except = Objects.Except.None )]
		class A { }
		class B : A { }

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		class /* ImmutableExceptionInheritanceIsInvalid(A,Set the [Immutable] exceptions on this type to Except.None.) */ InheritingFromBaseOfBaseType /**/ : B { }

	}

	class MultipleInheritance {

		[Objects.Immutable( Except = Objects.Except.ItsUgly | Objects.Except.ItHasntBeenLookedAt )]
		interface IFoo { }
		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		interface IBaz { }

		class MultipleInheritanceNoExceptions : IFoo, IBaz { }

		[Objects.Immutable( Except = Objects.Except.ItsUgly )]
		class MultipleInheritanceSubsetOfBoth : IFoo, IBaz { }

		[Objects.Immutable( Except = Objects.Except.ItHasntBeenLookedAt )]
		class /* ImmutableExceptionInheritanceIsInvalid(IBaz,Reduce the [Immutable] exceptions on this type to a subset of { ItsUgly }.) */ MultipleInheritanceSubsetOfOne /**/ : IFoo, IBaz { }

	}

}