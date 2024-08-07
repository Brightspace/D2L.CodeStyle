// analyzer: D2L.CodeStyle.Analyzers.Language.ClassShouldBeSealedAnalyzer, D2L.CodeStyle.Analyzers

namespace D2L.CodeStyle.Analyzers.Specs {
	// no diagnostic because they're public
	public class PublicClass { }
	public record PublicRecord { }
	public record class PublicRecordClass { }

	internal abstract class AbstractClass { } // no diagnostic because its abstract (but it is dead code)
	interface ISomeInterface { } // interfaces can't be sealed
	internal struct Struct { } // structs are implicitly sealed
	internal static class StaticClass { }

	// already sealed
	internal sealed class SealedClass { } // already sealed
	internal sealed record SealedRecord { }
	internal sealed record SealedRecordClass { }

	internal class /* ClassShouldBeSealed */ InternalUnsealedClass /**/ {
		private class /* ClassShouldBeSealed */ PrivateUnsealedClass /**/ { }
	}

	internal record /* ClassShouldBeSealed */ InternalUnsealedRecord /**/ {
		private record /* ClassShouldBeSealed */ PrivateUnsealedRecord /**/ { }
	}

	internal record class /* ClassShouldBeSealed */ InternalUnsealedRecordClass /**/ {
		private record class /* ClassShouldBeSealed */ PrivateUnsealedRecordClass /**/ { }
	}

	internal record /* ClassShouldBeSealed */ InternalUnsealedRecordWithArg /**/ ( int Arg ) {
		private record /* ClassShouldBeSealed */ PrivateUnsealedRecordWithArg /**/ ( int Arg ) { }
	}

	internal record class /* ClassShouldBeSealed */ InternalUnsealedRecordClassWithArg /**/ ( int Arg ) {
		private record class /* ClassShouldBeSealed */ PrivateUnsealedRecordClassWithArg /**/ ( int Arg ) { }
	}

	// internal/private classes can be unsealed if they are actually used as a base somewhere
	internal class InternalBaseClass {
		private class PrivateBaseBaseClass { }

		private class PrivateBaseClass : PrivateBaseBaseClass { }

		private sealed class Derived : PrivateBaseClass { }
	}

	internal sealed class Derived : InternalBaseClass {}

	internal class AnotherInternalBaseBaseClass { } // this one is used (albeit not usefully) as a base class
	internal class /* ClassShouldBeSealed */ AnotherInternalBaseClass /**/ : AnotherInternalBaseBaseClass { }

	// Controversial opinion: there seems to be a significant amount of
	// inappopriate use of protected in our code base. A large reason is a form
	// of code-gen that no longer exists that would create derived types for
	// webpage classes. We'll be ruthless and call this an error too.
	internal class /* ClassShouldBeSealed */ ThingWithProtectedMembers /**/ {
		protected int x;

		protected ThingWithProtectedMembers() {}

		public virtual void Foo() {}
	}
}
