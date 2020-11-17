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

	public sealed class Types {

		public interface RegularInterface { }

		[Immutable]
		public interface InterfaceMarkedImmutable { }


		public class RegularClass {

			private static int m_staticWritableImmutableField = 0;

			private static readonly int m_staticReadOnlyImmutableField = 0;

			private int m_writableImmutableField = 0;

			private readonly int m_readonlyImmutableField = 0;

		}
		public class RegularExtension : RegularClass { }
		public sealed class RegularSealedExtension : RegularClass { }

		[ImmutableBaseClass]
		public class SomeImmutableBaseClass { }
		static SomeImmutableBaseClass FuncReturningSomeImmutableBaseClass() => null
		public sealed class MutableExtensionOfSomeImmutableBaseClass : SomeImmutableBaseClass { }

		[Immutable]
		public sealed class ClassMarkedImmutableImplementingRegularInterface : RegularInterface { }

		[ImmutableBaseClass]
		public class ClassMarkedImmutableBaseClassImplementingRegularInterface : RegularInterface { }

	}

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutable {

		static int m_staticWritableImmutableField = 0;

		static readonly int m_staticReadOnlyImmutableField = 0;

		int /* MemberIsNotReadOnly(Field, m_writableImmutableField, AnalyzedClassMarkedImmutable) */ m_writableImmutableField /**/ = 0;

		readonly int m_readonlyImmutableField = 0;

		int AutoImplementedImmutableProperty { get; } = 0;

		int ImplementedImmutableProperty { get { return 0; } }



		static Types.RegularClass m_staticWritableMutableField = new Types.RegularClass();

		static readonly Types.RegularClass m_staticReadOnlyMutableField = new Types.RegularClass();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_writableMutableField, AnalyzedClassMarkedImmutable) */ m_writableMutableField /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		readonly Types.RegularClass m_readonlyMutableField = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		Types.RegularClass AutoImplementedMutableProperty { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		Types.RegularClass ImplementedMutableProperty { get { return new Types.RegularClass(); } }



		static Types.RegularClass m_staticWritableMutableFieldHeldAsMutableSuper = new Types.RegularExtension();

		static readonly Types.RegularClass m_staticReadOnlyMutableFieldHeldAsMutableSuper = new Types.RegularExtension();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_writableMutableFieldHeldAsMutableSuper, AnalyzedClassMarkedImmutable) */ m_writableMutableFieldHeldAsMutableSuper /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		readonly Types.RegularClass m_readonlyMutableFieldHeldAsMutableSuper = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		Types.RegularClass AutoImplementedMutablePropertyHeldAsMutableSuper { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		Types.RegularClass ImplementedMutablePropertyAsMutableSuper { get { return new Types.RegularExtension(); } }



		static Types.RegularClass m_staticWritableSealedMutableFieldHeldAsMutableSuper = new Types.RegularSealedExtension();

		static readonly Types.RegularClass m_staticReadOnlySealedMutableFieldHeldAsMutableSuper = new Types.RegularSealedExtension();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_writableSealedMutableFieldHeldAsMutableSuper, AnalyzedClassMarkedImmutable) */ m_writableSealedMutableFieldHeldAsMutableSuper /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		readonly Types.RegularClass m_readonlySealedMutableFieldHeldAsMutableSuper = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		Types.RegularClass AutoImplementedSealedMutablePropertyHeldAsMutableSuper { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		Types.RegularClass ImplementedSealedMutablePropertyAsMutableSuper { get { return new Types.RegularSealedExtension(); } }



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_writeableImmutableBaseClassFieldWithImmutableBaseClassInitializer, AnalyzedClassMarkedImmutable) */ m_writeableImmutableBaseClassFieldWithImmutableBaseClassInitializer /**/ = new Types.SomeImmutableBaseClass();

		readonly Types.SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithImmutableBaseClassInitializer = new Types.SomeImmutableBaseClass();

		Types.SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithImmutableBaseClassInitializer { get; } = new Types.SomeImmutableBaseClass();



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_writeableImmutableBaseClassFieldWithMutableInitializer, AnalyzedClassMarkedImmutable) */ m_writeableImmutableBaseClassFieldWithMutableInitializer /**/ = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();

		readonly Types.SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithMutableInitializer = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();

		Types.SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithMutableInitializer { get; } = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();



		readonly Types.SomeImmutableBaseClass m_readonlyImmutableBaseClassFieldWithImmutableBaseClassFuncInitializer = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;

		Types.SomeImmutableBaseClass AutoImplementedImmutableBaseClassFieldWithImmutableBaseClassFuncInitializer { get; } = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;



		static Types.RegularInterface m_staticWritableMutableFieldImmutableInitializer = new Types.ClassMarkedImmutableImplementingRegularInterface();

		static readonly Types.RegularInterface m_staticReadOnlyMutableFieldImmutableInitializer = new Types.ClassMarkedImmutableImplementingRegularInterface();

		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_writableMutableFieldImmutableInitializer, AnalyzedClassMarkedImmutable) */ m_writableMutableFieldImmutableInitializer /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();

		readonly Types.RegularInterface m_readonlyMutableFieldImmutableInitializer = new Types.ClassMarkedImmutableImplementingRegularInterface();

		Types.RegularInterface AutoImplementedMutablePropertyImmutableInitializer { get; } = new Types.ClassMarkedImmutableImplementingRegularInterface();



		static Types.RegularInterface m_staticWritableMutableFieldImmutableBaseClassInitializer = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		static readonly Types.RegularInterface m_staticReadOnlyMutableFieldImmutableBaseClassInitializer = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_writableMutableFieldImmutableBaseClassInitializer, AnalyzedClassMarkedImmutable) */ m_writableMutableFieldImmutableBaseClassInitializer /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		readonly Types.RegularInterface m_readonlyMutableFieldImmutableBaseClassInitializer = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		Types.RegularInterface AutoImplementedMutablePropertyImmutableBaseClassInitializer { get; } = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
	}

}
