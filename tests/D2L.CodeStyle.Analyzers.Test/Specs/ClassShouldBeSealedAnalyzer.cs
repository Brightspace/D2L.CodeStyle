// analyzer: D2L.CodeStyle.Analyzers.Language.ClassShouldBeSealedAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs {
	public class PublicClass { } // no diagnostic because its public
	internal abstract class AbstractClass { } // no diagnostic because its abstract (but it is dead code)
	interface ISomeInterface { } // interfaces can't be sealed
	internal struct Struct { } // structs are implicitly sealed
	internal static class StaticClass { }

	internal sealed class SealedClass { } // already sealed

	internal class /* ClassShouldBeSealed */ InternalUnsealedClass /**/ {
		private class /* ClassShouldBeSealed */ PrivateUnsealedClass /**/ { }
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
