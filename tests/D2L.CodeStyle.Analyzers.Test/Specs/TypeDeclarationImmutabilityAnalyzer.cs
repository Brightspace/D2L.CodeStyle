﻿// analyzer: D2L.CodeStyle.Analyzers.Immutability.TypeDeclarationImmutabilityAnalyzer

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
		public interface SomeImmutableInterface { }


		public class RegularClass {

			private static int m_field = 0;

			private static readonly int m_field = 0;

			private int m_field = 0;

			private readonly int m_field = 0;

		}
		public class RegularExtension : RegularClass { }
		public sealed class RegularSealedExtension : RegularClass { }

		[ImmutableBaseClass]
		public class SomeImmutableBaseClass { }
		static SomeImmutableBaseClass FuncReturningSomeImmutableBaseClass() => null
		public sealed class MutableExtensionOfSomeImmutableBaseClass : SomeImmutableBaseClass { }

		[Immutable]
		public class SomeImmutableClass { }

		[Immutable]
		public sealed class ClassMarkedImmutableImplementingRegularInterface : RegularInterface { }

		[ImmutableBaseClass]
		public class ClassMarkedImmutableBaseClassImplementingRegularInterface : RegularInterface { }

	}

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingMutableClass : /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ { }

	[ImmutableBaseClass]
	public sealed class AnalyzedClassMarkedImmutableBaseClassExtendingMutableClass : /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ { }

	[ImmutableBaseClass]
	public sealed class AnalyzedClassMarkedImmutableBaseClassExtendingImmutableBaseClass : Types.SomeImmutableBaseClass { }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingImmutableBaseClass : Types.SomeImmutableBaseClass { }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingImmutableClass : Types.SomeImmutableClass { }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutable {



		class SomeEventArgs { }
		delegate void SomeEventHandler( object sender, SomeEventArgs e );
		/* EventMemberMutable() */ event SomeEventHandler SomeEvent; /**/



		static int m_field = 0;

		static readonly int m_field = 0;

		int /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = 0;

		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int m_field = 0;

		[Mutability.Audited]
		int m_field = 0;

		readonly int m_field = 0;

		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Unaudited( Because.ItHasntBeenLookedAt ) /**/]
		readonly int m_field = 0;

		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Audited /**/]
		readonly int m_field = 0;

		int Property { get; } = 0;

		int Property { get { return 0; } }



		static object m_field = null;
		static readonly object m_field = null;
		object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = null;
		readonly object m_field = null;
		object Property { get; } = null;
		object Property { get { return null; } }



		static object m_field = new object();
		static readonly object m_field = new object();
		object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new object();
		readonly object m_field = new object();
		object Property { get; } = new object();
		object Property { get { return new object(); } }



		static Func<object> m_field = () => null;
		static readonly Func<object> m_field = () => null;
		Func<object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = () => null;
		readonly Func<object> m_field = () => null;
		Func<object> Property { get; } = () => null;
		Func<object> Property { get { return () => null; } }



		static Types.RegularClass m_field = new Types.RegularClass();

		static readonly Types.RegularClass m_field = new Types.RegularClass();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		readonly Types.RegularClass m_field = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.RegularClass m_field = new Types.RegularClass ();

		[Mutability.Audited]
		readonly Types.RegularClass m_field = new Types.RegularClass();

		Types.RegularClass Property { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ ();

		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.RegularClass Property { get; } = new Types.RegularClass ();

		[Mutability.Audited]
		Types.RegularClass Property { get; } = new Types.RegularClass();

		Types.RegularClass Property { get { return new Types.RegularClass(); } }



		static Types.RegularClass m_field = new Types.RegularExtension();

		static readonly Types.RegularClass m_field = new Types.RegularExtension();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		readonly Types.RegularClass m_field = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		Types.RegularClass Property { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularExtension,  (or [ImmutableBaseClass])) */ Types.RegularExtension /**/ ();

		Types.RegularClass Property { get { return new Types.RegularExtension(); } }



		static Types.RegularClass m_field = new Types.RegularSealedExtension();

		static readonly Types.RegularClass m_field = new Types.RegularSealedExtension();

		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		readonly Types.RegularClass m_field = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		Types.RegularClass Property { get; } = new /* NonImmutableTypeHeldByImmutable(Class, RegularSealedExtension, ) */ Types.RegularSealedExtension /**/ ();

		Types.RegularClass Property { get { return new Types.RegularSealedExtension(); } }



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.SomeImmutableBaseClass();

		readonly Types.SomeImmutableBaseClass m_field = new Types.SomeImmutableBaseClass();

		Types.SomeImmutableBaseClass Property { get; } = new Types.SomeImmutableBaseClass();



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();

		readonly Types.SomeImmutableBaseClass m_field = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();

		Types.SomeImmutableBaseClass Property { get; } = new /* NonImmutableTypeHeldByImmutable(Class, MutableExtensionOfSomeImmutableBaseClass, ) */ Types.MutableExtensionOfSomeImmutableBaseClass /**/ ();



		readonly Types.SomeImmutableBaseClass m_field = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;

		Types.SomeImmutableBaseClass Property { get; } = /* NonImmutableTypeHeldByImmutable(Class, SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;



		static Types.RegularInterface m_field = new Types.ClassMarkedImmutableImplementingRegularInterface();

		static readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableImplementingRegularInterface();

		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();

		readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableImplementingRegularInterface();

		Types.RegularInterface Property { get; } = new Types.ClassMarkedImmutableImplementingRegularInterface();



		static Types.RegularInterface m_field = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		static readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();

		Types.RegularInterface Property { get; } = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
	}

}
