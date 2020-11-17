// analyzer: D2L.CodeStyle.Analyzers.Immutability.TypeDeclarationImmutabilityAnalyzer

using System;
using System.Security.Cryptography.X509Certificates;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

#region Relevant Types

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute {}
		public sealed class Immutable : ImmutableAttributeBase { }
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }
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

#endregion

namespace SpecTests {

	using static Immutable = Objects.Immutable;
	using static ImmutableBaseClass = Objects.ImmutableBaseClassAttribute;

	public sealed class Interfaces {

		public interface RegularInterface { }

		[Immutable]
		public interface InterfaceMarkedImmutable { }

	}

	public sealed class Classes {

		public class RegularClass {

			private static int m_staticWritableImmutableField = 0;

			private static readonly int m_staticReadOnlyImmutableField = 0;

			private int m_writableImmutableField = 0;

			private readonly int m_readonlyImmutableField = 0;

		}

		public class RegularExtension : RegularClass { }
		public sealed class RegularSealedExtension : RegularClass { }

		[Immutable]
		public sealed class ClassMarkedImmutableImplementingRegularInterface : Interfaces.RegularInterface { }

		[ImmutableBaseClass]
		public class ClassMarkedImmutableBaseClassImplementingRegularInterface : Interfaces.RegularInterface { }

		[ImmutableBaseClass]
		public class SomeImmutableBaseClass { }

		static SomeImmutableBaseClass FuncReturningSomeImmutableBaseClass() => null

		public sealed class MutableExtensionOfSomeImmutableBaseClass : SomeImmutableBaseClass { }

		[Immutable]
		public sealed class ClassMarkedImmutable {

			static int m_staticWritableImmutableField = 0;

			static readonly int m_staticReadOnlyImmutableField = 0;

			int /* MemberIsNotReadOnly(Field, m_writableImmutableField, ClassMarkedImmutable) */ m_writableImmutableField /**/ = 0;

			readonly int m_readonlyImmutableField = 0;

			int AutoImplementedImmutableProperty { get; } = 0;

			int ImplementedImmutableProperty { get { return 0; } }



			static RegularClass m_staticWritableMutableField = new RegularClass();

			static readonly RegularClass m_staticReadOnlyMutableField = new RegularClass();

			RegularClass /* MemberIsNotReadOnly(Field, m_writableMutableField, ClassMarkedImmutable) */ m_writableMutableField /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ RegularClass /**/ ();

			readonly RegularClass m_readonlyMutableField = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ RegularClass /**/ ();

			RegularClass AutoImplementedMutableProperty { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ RegularClass /**/ ();

			RegularClass ImplementedMutableProperty { get { return new RegularClass(); } }



			static RegularClass m_staticWritableMutableFieldHeldAsMutableSuper = new RegularExtension();

			static readonly RegularClass m_staticReadOnlyMutableFieldHeldAsMutableSuper = new RegularExtension();

			RegularClass /* MemberIsNotReadOnly(Field, m_writableMutableFieldHeldAsMutableSuper, ClassMarkedImmutable) */ m_writableMutableFieldHeldAsMutableSuper /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ RegularExtension /**/ ();

			readonly RegularClass m_readonlyMutableFieldHeldAsMutableSuper = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ RegularExtension /**/ ();

			RegularClass AutoImplementedMutablePropertyHeldAsMutableSuper { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ RegularExtension /**/ ();

			RegularClass ImplementedMutablePropertyAsMutableSuper { get { return new RegularExtension(); } }



			static RegularClass m_staticWritableSealedMutableFieldHeldAsMutableSuper = new RegularSealedExtension();

			static readonly RegularClass m_staticReadOnlySealedMutableFieldHeldAsMutableSuper = new RegularSealedExtension();

			RegularClass /* MemberIsNotReadOnly(Field, m_writableSealedMutableFieldHeldAsMutableSuper, ClassMarkedImmutable) */ m_writableSealedMutableFieldHeldAsMutableSuper /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ RegularSealedExtension /**/ ();

			readonly RegularClass m_readonlySealedMutableFieldHeldAsMutableSuper = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ RegularSealedExtension /**/ ();

			RegularClass AutoImplementedSealedMutablePropertyHeldAsMutableSuper { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ RegularSealedExtension /**/ ();

			RegularClass ImplementedSealedMutablePropertyAsMutableSuper { get { return new RegularSealedExtension(); } }



			static Interfaces.RegularInterface m_staticWritableMutableFieldImmutableInitializer = new ClassMarkedImmutableImplementingRegularInterface();

			static readonly Interfaces.RegularInterface m_staticReadOnlyMutableFieldImmutableInitializer = new ClassMarkedImmutableImplementingRegularInterface();

			Interfaces.RegularInterface /* MemberIsNotReadOnly(Field, m_writableMutableFieldImmutableInitializer, ClassMarkedImmutable) */ m_writableMutableFieldImmutableInitializer /**/ = new ClassMarkedImmutableImplementingRegularInterface();

			readonly Interfaces.RegularInterface m_readonlyMutableFieldImmutableInitializer = new ClassMarkedImmutableImplementingRegularInterface();

			Interfaces.RegularInterface AutoImplementedMutablePropertyImmutableInitializer { get; } = new ClassMarkedImmutableImplementingRegularInterface();



			static Interfaces.RegularInterface m_staticWritableMutableFieldImmutableBaseClassInitializer = new ClassMarkedImmutableBaseClassImplementingRegularInterface();

			static readonly Interfaces.RegularInterface m_staticReadOnlyMutableFieldImmutableBaseClassInitializer = new ClassMarkedImmutableBaseClassImplementingRegularInterface();

			Interfaces.RegularInterface /* MemberIsNotReadOnly(Field, m_writableMutableFieldImmutableBaseClassInitializer, ClassMarkedImmutable) */ m_writableMutableFieldImmutableBaseClassInitializer /**/ = new ClassMarkedImmutableBaseClassImplementingRegularInterface();

			readonly Interfaces.RegularInterface m_readonlyMutableFieldImmutableBaseClassInitializer = new ClassMarkedImmutableBaseClassImplementingRegularInterface();

			Interfaces.RegularInterface AutoImplementedMutablePropertyImmutableBaseClassInitializer { get; } = new ClassMarkedImmutableBaseClassImplementingRegularInterface();



			SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_writeableImmutableBaseClassFieldWithImmutableBaseClassInitializer, ClassMarkedImmutable) */ m_writeableImmutableBaseClassFieldWithImmutableBaseClassInitializer /**/ = new SomeImmutableBaseClass();

			readonly SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithImmutableBaseClassInitializer = new SomeImmutableBaseClass();

			SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithImmutableBaseClassInitializer { get; } = new SomeImmutableBaseClass();



			SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_writeableImmutableBaseClassFieldWithMutableInitializer, ClassMarkedImmutable) */ m_writeableImmutableBaseClassFieldWithMutableInitializer /**/ = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ MutableExtensionOfSomeImmutableBaseClass /**/ ();

			readonly SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithMutableInitializer = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ MutableExtensionOfSomeImmutableBaseClass /**/ ();

			SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithMutableInitializer { get; } = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ MutableExtensionOfSomeImmutableBaseClass /**/ () ;



			readonly SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithImmutableBaseClassFuncInitializer = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ FuncReturningSomeImmutableBaseClass() /**/;

			SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithImmutableBaseClassFuncInitializer { get; } = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ FuncReturningSomeImmutableBaseClass() /**/;
		}

	}

}
