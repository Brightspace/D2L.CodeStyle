// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;

#region Relevant Types

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices {
	// Polyfill for .NET framework
    internal static class IsExternalInit {}
}
#endif

#endregion


namespace SpecTests {

	public sealed class Types {

		public enum SomeEnum {
			Foo
		}

		public interface RegularInterface { }

		[Immutable]
		public interface SomeImmutableInterface { }

		#region Constructor immutability
		[Immutable]
		public sealed class Good : RegularInterface { }

		public class Bad : RegularInterface { }

		[Immutable]
		public sealed class SomeClassWithConstructor1 {
			public readonly RegularInterface m_interface = new Good();

			public SomeClassWithConstructor1() {
				m_interface = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.Bad,  (or [ImmutableBaseClass])) */ new Bad() /**/;
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor2 {
			public readonly RegularInterface m_interface = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.Bad,  (or [ImmutableBaseClass])) */ new Bad() /**/;

			public SomeClassWithConstructor2() {
				m_interface = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.Bad,  (or [ImmutableBaseClass])) */ new Bad() /**/;
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor3 {
			public readonly RegularInterface m_interface = new Good();

			public SomeClassWithConstructor3() {
				m_interface = new Good();
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor4 {
			public readonly RegularInterface m_interface = new Good();

			public SomeClassWithConstructor4() {
				if( true == false ) {
					m_interface = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.Bad,  (or [ImmutableBaseClass])) */ new Bad() /**/;
				}
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor5 {
			public readonly int m_int = 5;
			public readonly object m_alwaysOk;
			public readonly object m_sometimesBad;

			public SomeClassWithConstructor5() {
				m_int = 29;

				m_alwaysOk = null;
				m_alwaysOk = new object();

				m_sometimesBad = null;
				m_sometimesBad = new object();
				m_sometimesBad = /* ArraysAreMutable(Int32) */ new[] { 3 } /**/;
				m_sometimesBad = new Good();
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor6 {
			public int /* MemberIsNotReadOnly(Field, m_int, SomeClassWithConstructor6) */ m_int /**/ = 5;

			public SomeClassWithConstructor6() {
				m_int = 29;
			}
		}

		[ConditionallyImmutable]
		internal sealed class SomeClassWithConstructor7<[ConditionallyImmutable.OnlyIf] T, U> where U : T {
			public T Tee { get; }

			public SomeClassWithConstructor7( U you ) {
				Tee = you;
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor8 {
			public readonly RegularInterface m_interface = new Good();

			public SomeClassWithConstructor8() {
				(m_interface) = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.Bad,  (or [ImmutableBaseClass])) */ new Bad() /**/;
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor9 {
			public readonly RegularInterface m_interface = new Good();
			public readonly RegularInterface m_interface2 = new Good();

			public SomeClassWithConstructor9() {
				/* UnknownImmutabilityAssignmentKind(Deconstructed assignment) */ (m_interface, m_interface2) /**/ = (new Good(), new Good());
			}
		}

		[Immutable]
		public sealed class SomeClassWithConstructor10 {
			public readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ RegularInterface /**/ m_interface;
			public readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ RegularInterface /**/ m_interface2;

			public SomeClassWithConstructor10() {
				/* UnknownImmutabilityAssignmentKind(Deconstructed assignment) */ (m_interface, m_interface2) /**/ = (new Good(), new Good());
			}
		}
		#endregion


		public class RegularClass {
			private const int m_const = 0;

			private int m_field1 = 0;

			private readonly int m_field2 = 0;

		}
		public class RegularExtension : RegularClass { }
		public sealed class RegularSealedExtension : RegularClass { }

		public class SomeMutableClassWithImmutableTypeParameter<[Immutable] T> { }

		[ImmutableBaseClass]
		public class SomeImmutableBaseClass { }
		public static SomeImmutableBaseClass FuncReturningSomeImmutableBaseClass() => null;
		public sealed class MutableExtensionOfSomeImmutableBaseClass : SomeImmutableBaseClass { }

		[ImmutableBaseClass]
		public class SomeImmutableGenericBaseClassRestrictingT<[Immutable] T> { }

		[Immutable]
		public class SomeImmutableClass { }
		public static SomeImmutableClass FuncReturningSomeImmutableClass() => null;

		[Immutable]
		public class SomeImmutableGenericClassRestrictingT<[Immutable] T> { }

		[Immutable]
		public sealed class ClassMarkedImmutableImplementingRegularInterface : RegularInterface { }

		[ImmutableBaseClass]
		public class ClassMarkedImmutableBaseClassImplementingRegularInterface : RegularInterface { }

		public readonly struct SomeStruct { }

		[Immutable]
		public readonly struct SomeImmutableStruct { }

		public interface SomeGenericInterface<T, U> { }

		[Immutable]
		public interface SomeImmutableGenericInterface<T, U> { }

		[ConditionallyImmutable]
		public interface SomeImmutableGenericInterfaceGivenT<[ConditionallyImmutable.OnlyIf] T, U> { }

		[ConditionallyImmutable]
		public interface SomeImmutableGenericInterfaceGivenU<T, [ConditionallyImmutable.OnlyIf] U> { }

		[ConditionallyImmutable]
		public interface SomeImmutableGenericInterfaceGivenTU<[ConditionallyImmutable.OnlyIf] T, [ConditionallyImmutable.OnlyIf] U> { }

		[Immutable]
		public interface SomeImmutableGenericInterfaceRestrictingT<[Immutable] T, U> { }

		[Immutable]
		public interface SomeImmutableGenericInterfaceRestrictingU<T, [Immutable] U> { }

		[Immutable]
		public interface SomeImmutableGenericInterfaceRestrictingTU<[Immutable] T, [Immutable] U> { }

		public static void SomeGenericMethod<T, U>() { }
		public static void SomeGenericMethodRestrictingT<[Immutable] T, U>() { }
		public static void SomeGenericMethodRestrictingU<T, [Immutable] U>() { }
		public static void SomeGenericMethodRestrictingTU<[Immutable] T, [Immutable] U>() { }
		public static void SomeGenericMethodRestrictingT<[Immutable] T>( T value ) { }


		/// <summary><see cref="SomeGenericMethodCref{T}()"/></summary>
		public static void SomeGenericMethodCref<[Immutable] T>( int x ) { }

}

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingMutableClass : /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ { }

	[ImmutableBaseClass]
	public sealed class AnalyzedClassMarkedImmutableBaseClassExtendingMutableClass : /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ Types.RegularClass /**/ { }

	[ImmutableBaseClass]
	public sealed class AnalyzedClassMarkedImmutableBaseClassExtendingImmutableBaseClass : Types.SomeImmutableBaseClass { }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingImmutableBaseClass : Types.SomeImmutableBaseClass { }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutableExtendingImmutableClass : Types.SomeImmutableClass { }

	[Immutable]
	public sealed class AnalyzedClassWithNonImmutableTypeParameterImplementingImmutableInterfaceParameter<T, U> : Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> { }

	public sealed class NonImmutableClassWithNonImmutableTypeParameterImplementingImmutableBaseClassParameter1<T> : Types.SomeImmutableGenericBaseClassRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> { }
	public sealed class NonImmutableClassWithNonImmutableTypeParameterImplementingImmutableBaseClassParameter2<T> : Types.SomeMutableClassWithImmutableTypeParameter</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> { }

	[Immutable]
	public sealed class AnalyzedClassWithNonImmutableTypeParameterImplementingImmutableBaseClassParameter<T> : Types.SomeImmutableGenericClassRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> { }

	[ Immutable]
	[ConditionallyImmutable]
	public sealed class /* ConflictingImmutability(Immutable, ConditionallyImmutable, class) */ AnalyzedClassMarkedWithMultipleImmutabilities1 /**/ { }

	[Immutable]
	[ImmutableBaseClassAttribute]
	public sealed class /* ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, class) */ AnalyzedClassMarkedWithMultipleImmutabilities2 /**/ { }

	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed class /* ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, class) */ AnalyzedClassMarkedWithMultipleImmutabilities3 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed class /* ConflictingImmutability(Immutable, ConditionallyImmutable, class)
	                     | ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, class)
	                     | ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, class) */ AnalyzedClassMarkedWithMultipleImmutabilities4 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	public interface /* ConflictingImmutability(Immutable, ConditionallyImmutable, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities1 /**/ { }

	[Immutable]
	[ImmutableBaseClassAttribute]
	public interface /* ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities2 /**/ { }

	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public interface /* ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities3 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public interface /* ConflictingImmutability(Immutable, ConditionallyImmutable, interface)
					         | ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, interface)
	                         | ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities4 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	public struct /* ConflictingImmutability(Immutable, ConditionallyImmutable, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities1 /**/ { }

	[Immutable]
	[ImmutableBaseClassAttribute]
	public struct /* ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities2 /**/ { }

	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public struct /* ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities3 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public struct /* ConflictingImmutability(Immutable, ConditionallyImmutable, struct)
	                 | ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, struct)
	                 | ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities4 /**/	{ }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutable {



		class SomeEventArgs { }
		delegate void SomeEventHandler( object sender, SomeEventArgs e );
		/* EventMemberMutable() */ event SomeEventHandler SomeEvent; /**/


		object this[ int index ] {
			get { return null; }
			set { return; }
		}

		public int X { get; init; }

		// This isn't safe because init-only properties can be overwritten by
		// any caller (which we may not be able to analyze).
		public /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ Y { get; init; } = new object();

		static int /* MemberIsNotReadOnly(Field, m_field1, AnalyzedClassMarkedImmutable) */ m_field1 /**/ = 0;
		static readonly int m_field2 = 0;
		int /* MemberIsNotReadOnly(Field, m_field3, AnalyzedClassMarkedImmutable) */ m_field3 /**/ = 0;
		int /* MemberIsNotReadOnly(Field, m_field4, AnalyzedClassMarkedImmutable) */ m_field4 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int m_field5 = 0;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		int m_field6 = 0;
		readonly int m_field7 = 0;
		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Unaudited( Because.ItHasntBeenLookedAt ) /**/]
		readonly int m_field8 = 0;
		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Audited( "John Doe", "1970-01-01", "Rationale" ) /**/]
		readonly int m_field9 = 0;
		readonly int m_field10;
		int Property1 { get; } = 0;
		int Property2 { get; }
		int Property3 { get { return 0; } }

		// target-typed new initializers
		readonly object m_lock = new();


		static Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field11, AnalyzedClassMarkedImmutable) */ m_field11 /**/ = Types.SomeEnum.Foo;
		static readonly Types.SomeEnum m_field12 = Types.SomeEnum.Foo;
		Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field13, AnalyzedClassMarkedImmutable) */ m_field13 /**/ = Types.SomeEnum.Foo;
		Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field14, AnalyzedClassMarkedImmutable) */ m_field14 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.SomeEnum m_field15 = Types.SomeEnum.Foo;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		Types.SomeEnum m_field16 = Types.SomeEnum.Foo;
		readonly Types.SomeEnum m_field17 = Types.SomeEnum.Foo;
		readonly Types.SomeEnum m_field18;
		Types.SomeEnum Property4 { get; } = Types.SomeEnum.Foo;
		Types.SomeEnum Property5 { get; }
		Types.SomeEnum Property6 { get { return Types.SomeEnum.Foo; } }



		static int[] /* MemberIsNotReadOnly(Field, m_field19, AnalyzedClassMarkedImmutable) */ m_field19 /**/ = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		static readonly int[] m_field20 = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		int[] /* MemberIsNotReadOnly(Field, m_field21, AnalyzedClassMarkedImmutable) */ m_field21 /**/ = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		/* ArraysAreMutable(Int32) */ int[] /**/ /* MemberIsNotReadOnly(Field, m_field22, AnalyzedClassMarkedImmutable) */ m_field22 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int[] m_field23 = new[] { 0 };
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		int[] m_field24 = new[] { 0 };
		readonly int[] m_field25 = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		readonly /* ArraysAreMutable(Int32) */ int[] /**/ m_field26;
		int[] Property7 { get; } = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		/* ArraysAreMutable(Int32) */ int[] /**/ Property8 { get; }
		int[] Property9 { get { return new[] { 0 }; } }



		static readonly /* UnexpectedTypeKind(PointerType) */ int* /**/ m_field27;
		/* UnexpectedTypeKind(PointerType) */ int* /**/ /* MemberIsNotReadOnly(Field, m_field28, AnalyzedClassMarkedImmutable) */ m_field28 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int* m_field29;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		int* m_field30;
		readonly /* UnexpectedTypeKind(PointerType) */ int* /**/ m_field31;
		/* UnexpectedTypeKind(PointerType) */ int* /**/ Property10 { get; }


		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ m_field32;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* MemberIsNotReadOnly(Field, m_field33, AnalyzedClassMarkedImmutable) */ m_field33 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ m_field34;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ Property11 { get; }


		static object /* MemberIsNotReadOnly(Field, m_field35, AnalyzedClassMarkedImmutable) */ m_field35 /**/ = null;
		static readonly object m_field36 = null;
		object /* MemberIsNotReadOnly(Field, m_field37, AnalyzedClassMarkedImmutable) */ m_field37 /**/ = null;
		readonly object m_field38 = null;
		object Property12 { get; } = null;
		object Property13 { get { return null; } }



		static object /* MemberIsNotReadOnly(Field, m_field38_2, AnalyzedClassMarkedImmutable) */ m_field38_2 /**/ = new object();
		static readonly object m_field39 = new object();
		object /* MemberIsNotReadOnly(Field, m_field40, AnalyzedClassMarkedImmutable) */ m_field40 /**/ = new object();
		readonly object m_field41 = new object();
		object Property14 { get; } = new object();
		object Property15 { get { return new object(); } }



		static readonly /* DynamicObjectsAreMutable */ dynamic /**/ m_field42;
		/* DynamicObjectsAreMutable */ dynamic /**/ /* MemberIsNotReadOnly(Field, m_field44, AnalyzedClassMarkedImmutable) */ m_field44 /**/;
		readonly /* DynamicObjectsAreMutable */ dynamic /**/ m_field45;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly dynamic m_field46;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly dynamic m_field47;
		/* DynamicObjectsAreMutable */ dynamic /**/ Property16 { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		dynamic Property17 { get; }
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		dynamic Property18 { get; }
		dynamic Property19 { get { return new ExpandoObject(); } }



		static Func<object> /* MemberIsNotReadOnly(Field, m_field48, AnalyzedClassMarkedImmutable) */ m_field48 /**/ = () => null;
		static readonly Func<object> m_field49 = () => null;
		Func<object> /* MemberIsNotReadOnly(Field, m_field50, AnalyzedClassMarkedImmutable) */ m_field50 /**/ = () => null;
		readonly Func<object> m_field51 = static () => null;
		Func<object> Property20 { get; } = static () => null;
		Func<object> Property21 { get { return () => null; } }



		static Func<object> /* MemberIsNotReadOnly(Field, m_field53, AnalyzedClassMarkedImmutable) */ m_field53 /**/ = () => { return null; };
		static readonly Func<object> m_field54 = () => { return null; };
		Func<object> /* MemberIsNotReadOnly(Field, m_field55, AnalyzedClassMarkedImmutable) */ m_field55 /**/ = () => { return null; };
		readonly Func<object> m_field56 = () => { return null; };
		Func<object> Property22 { get; } = static () => { return null; };
		Func<object> Property23 { get { return () => { return null; }; } }



		static (int, int) /* MemberIsNotReadOnly(Field, m_field57, AnalyzedClassMarkedImmutable) */ m_field57 /**/;
		static readonly (int, int) m_field58;
		(int, int) /* MemberIsNotReadOnly(Field, m_field59, AnalyzedClassMarkedImmutable) */ m_field59 /**/;
		readonly (int, int) m_field60;
		(int, int) Property24 { get; }
		(int, int) Property25 { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ /* MemberIsNotReadOnly(Field, m_field61, AnalyzedClassMarkedImmutable) */ m_field61 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ m_field62;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ /* MemberIsNotReadOnly(Field, m_field63, AnalyzedClassMarkedImmutable) */ m_field63 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ m_field64;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ Property26 { get; }
		(object, int) Property27 { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ /* MemberIsNotReadOnly(Field, m_field65, AnalyzedClassMarkedImmutable) */ m_field65 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ m_field66;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ /* MemberIsNotReadOnly(Field, m_field68, AnalyzedClassMarkedImmutable) */ m_field68 /**/;
		readonly  /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ m_field69;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ Property { get; }
		(int, object) Property28 { get { return default; } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field70, AnalyzedClassMarkedImmutable) */ m_field70 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		static readonly Types.RegularClass m_field71 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field72, AnalyzedClassMarkedImmutable) */ m_field72 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		readonly Types.RegularClass m_field73 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.RegularClass m_field74 = new Types.RegularClass ();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly Types.RegularClass m_field75 = new Types.RegularClass();
		Types.RegularClass Property29 { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.RegularClass Property30 { get; } = new Types.RegularClass ();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		Types.RegularClass Property31 { get; } = new Types.RegularClass();
		Types.RegularClass Property32 { get { return new Types.RegularClass(); } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field76, AnalyzedClassMarkedImmutable) */ m_field76 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		static readonly Types.RegularClass m_field77 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field78, AnalyzedClassMarkedImmutable) */ m_field78 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		readonly Types.RegularClass m_field79 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass Property33 { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass Property34 { get { return new Types.RegularExtension(); } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field80, AnalyzedClassMarkedImmutable) */ m_field80 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		static readonly Types.RegularClass m_field81 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field82, AnalyzedClassMarkedImmutable) */ m_field82 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		readonly Types.RegularClass m_field83 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass Property35 { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass Property36 { get { return new Types.RegularSealedExtension(); } }



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field84, AnalyzedClassMarkedImmutable) */ m_field84 /**/ = new Types.SomeImmutableBaseClass();
		readonly Types.SomeImmutableBaseClass m_field85 = new Types.SomeImmutableBaseClass();
		Types.SomeImmutableBaseClass Property37 { get; } = new Types.SomeImmutableBaseClass();



		readonly Types.SomeImmutableBaseClass m_field86 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;
		Types.SomeImmutableBaseClass Property38 { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field87, AnalyzedClassMarkedImmutable) */ m_field87 /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;
		readonly Types.SomeImmutableBaseClass m_field88 = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;
		Types.SomeImmutableBaseClass Property39 { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;



		Types.SomeImmutableClass /* MemberIsNotReadOnly(Field, m_field89, AnalyzedClassMarkedImmutable) */ m_field89 /**/ = new Types.SomeImmutableClass();
		readonly Types.SomeImmutableClass m_field90 = new Types.SomeImmutableClass();
		Types.SomeImmutableClass Property40 { get; } = new Types.SomeImmutableClass();



		readonly Types.SomeImmutableClass m_field91 = Types.FuncReturningSomeImmutableClass();
		Types.SomeImmutableClass Property41 { get; } = Types.FuncReturningSomeImmutableClass();



		static /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ /* MemberIsNotReadOnly(Field, m_field92, AnalyzedClassMarkedImmutable) */ m_field92 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ m_field93;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ /* MemberIsNotReadOnly(Field, m_field94, AnalyzedClassMarkedImmutable) */ m_field94 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ m_field95;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.RegularInterface m_field96;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly Types.RegularInterface m_field97;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ Property42 { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.RegularInterface Property43 { get; }
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		Types.RegularInterface Property44 { get; }



		static Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field98, AnalyzedClassMarkedImmutable) */ m_field98 /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();
		static readonly Types.RegularInterface m_field99 = new Types.ClassMarkedImmutableImplementingRegularInterface();
		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field100, AnalyzedClassMarkedImmutable) */ m_field100 /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();
		readonly Types.RegularInterface m_field101 = new Types.ClassMarkedImmutableImplementingRegularInterface();
		Types.RegularInterface Property45 { get; } = new Types.ClassMarkedImmutableImplementingRegularInterface();



		static Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field102, AnalyzedClassMarkedImmutable) */ m_field102 /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		static readonly Types.RegularInterface m_field103 = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field104, AnalyzedClassMarkedImmutable) */ m_field104 /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		readonly Types.RegularInterface m_field105 = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		Types.RegularInterface Property46 { get; } = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();



		static /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ /* MemberIsNotReadOnly(Field, m_field106, AnalyzedClassMarkedImmutable) */ m_field106 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ m_field107;
		/* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ /* MemberIsNotReadOnly(Field, m_field108, AnalyzedClassMarkedImmutable) */ m_field108 /**/;
		Types.SomeStruct /* MemberIsNotReadOnly(Field, m_field109, AnalyzedClassMarkedImmutable) */ m_field109 /**/ = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		readonly /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ m_field110;
		readonly Types.SomeStruct  m_field111 = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.SomeStruct m_field112;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly Types.SomeStruct m_field113;
		/* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ Property47 { get; }
		Types.SomeStruct Property48 { get; } = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.SomeStruct Property49 { get; }
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		Types.SomeStruct Property50 { get; }



		static Types.SomeImmutableStruct /* MemberIsNotReadOnly(Field, m_field114, AnalyzedClassMarkedImmutable) */ m_field114 /**/;
		static readonly Types.SomeImmutableStruct m_field115;
		Types.SomeImmutableStruct /* MemberIsNotReadOnly(Field, m_field116, AnalyzedClassMarkedImmutable) */ m_field116 /**/;
		readonly Types.SomeImmutableStruct m_field117;
		Types.SomeImmutableStruct Property51 { get; }



		static /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ /* MemberIsNotReadOnly(Field, m_field118, AnalyzedClassMarkedImmutable) */ m_field118 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ m_field119;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ /* MemberIsNotReadOnly(Field, m_field120, AnalyzedClassMarkedImmutable) */ m_field120 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ m_field121;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ Property52 { get; }
		Types.SomeGenericInterface<int, int> Property53 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenT<int, object> /* MemberIsNotReadOnly(Field, m_field122, AnalyzedClassMarkedImmutable) */ m_field122 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenT<int, object> m_field123;
		Types.SomeImmutableGenericInterfaceGivenT<int, object> /* MemberIsNotReadOnly(Field, m_field124, AnalyzedClassMarkedImmutable) */ m_field124 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<int, object> m_field125;
		Types.SomeImmutableGenericInterfaceGivenT<int, object> Property54 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<int, object> Property55 { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field126, AnalyzedClassMarkedImmutable) */ m_field126 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ m_field127;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field128, AnalyzedClassMarkedImmutable) */ m_field128 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ m_field129;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ Property56 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<object, int> Property57 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceGivenU<object, int> /* MemberIsNotReadOnly(Field, m_field130, AnalyzedClassMarkedImmutable) */ m_field130 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenU<object, int> m_field131;
		Types.SomeImmutableGenericInterfaceGivenU<object, int> /* MemberIsNotReadOnly(Field, m_field132, AnalyzedClassMarkedImmutable) */ m_field132 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<object, int> m_field133;
		Types.SomeImmutableGenericInterfaceGivenU<object, int> Property58 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<object, int> Property59 { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field134, AnalyzedClassMarkedImmutable) */ m_field134 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ m_field135;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field136, AnalyzedClassMarkedImmutable) */ m_field136 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ m_field137;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ Property60 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<int, object> Property61 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenTU<int, int> /* MemberIsNotReadOnly(Field, m_field138, AnalyzedClassMarkedImmutable) */ m_field138 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenTU<int, int> m_field139;
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> /* MemberIsNotReadOnly(Field, m_field140, AnalyzedClassMarkedImmutable) */ m_field140 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<int, int> m_field141;
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> Property62 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> Property63 { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field142, AnalyzedClassMarkedImmutable) */ m_field142 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ m_field143;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field144, AnalyzedClassMarkedImmutable) */ m_field144 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ m_field145;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ Property64 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<int, object> Property65 { get { return default; } }


		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field146, AnalyzedClassMarkedImmutable) */ m_field146 /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ m_field147;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field148, AnalyzedClassMarkedImmutable) */ m_field148 /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ m_field149;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ Property66 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<object, int> Property67 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT<int, object> /* MemberIsNotReadOnly(Field, m_field150, AnalyzedClassMarkedImmutable) */ m_field150 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT<int, object> m_field151;
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> /* MemberIsNotReadOnly(Field, m_field151_2, AnalyzedClassMarkedImmutable) */ m_field151_2 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT<int, object> m_field152;
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> Property68 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> Property69 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field153, AnalyzedClassMarkedImmutable) */ m_field153 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field154;
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field155, AnalyzedClassMarkedImmutable) */ m_field155 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field156;
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property70 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property71 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<object, int> /* MemberIsNotReadOnly(Field, m_field157, AnalyzedClassMarkedImmutable) */ m_field157 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<object, int> m_field158;
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> /* MemberIsNotReadOnly(Field, m_field159, AnalyzedClassMarkedImmutable) */ m_field159 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<object, int> m_field160;
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> Property72 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> Property73 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field161, AnalyzedClassMarkedImmutable) */ m_field161 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field162;
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field163, AnalyzedClassMarkedImmutable) */ m_field163 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field164;
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property74 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property75 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> /* MemberIsNotReadOnly(Field, m_field165, AnalyzedClassMarkedImmutable) */ m_field165 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> m_field166;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> /* MemberIsNotReadOnly(Field, m_field167, AnalyzedClassMarkedImmutable) */ m_field167 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> m_field168;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> Property76 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> Property77 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field169, AnalyzedClassMarkedImmutable) */ m_field169 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field170;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field171, AnalyzedClassMarkedImmutable) */ m_field171 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field172;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property78 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property79 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field173, AnalyzedClassMarkedImmutable) */ m_field173 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field174;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int>/* MemberIsNotReadOnly(Field, m_field175, AnalyzedClassMarkedImmutable) */ m_field175 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int>m_field176;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property80 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property81 { get { return default; } }

		private readonly IEnumerable<int> m_enumerable = Enumerable.Empty<int>();
		private readonly int[] m_array = Array.Empty<int>();
	}

	[Immutable]
	public sealed class AnalyzedImmutableGenericClassRestrictingT<[Immutable] T, U>
		where T : new()
		where U : new()
	{



		static T /* MemberIsNotReadOnly(Field, m_field177, AnalyzedImmutableGenericClassRestrictingT) */ m_field177 /**/;
		static readonly T m_field178;
		T /* MemberIsNotReadOnly(Field, m_field179, AnalyzedImmutableGenericClassRestrictingT) */ m_field179 /**/;
		readonly T m_field180;
		T Property82 { get; }
		T Property83 { get { return default; } }



		static T /* MemberIsNotReadOnly(Field, m_field181, AnalyzedImmutableGenericClassRestrictingT) */ m_field181 /**/ = new T();
		static readonly T m_field182 = new T();
		T /* MemberIsNotReadOnly(Field, m_field183, AnalyzedImmutableGenericClassRestrictingT) */ m_field183 /**/ = new T();
		readonly T m_field184 = new T();
		T Property84 { get; } = new T();
		T Property85 { get { return new T(); } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field185, AnalyzedImmutableGenericClassRestrictingT) */ m_field185 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field186;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field187, AnalyzedImmutableGenericClassRestrictingT) */ m_field187 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field188;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U m_field189;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field190;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field191;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly U m_field192;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ Property86 { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property87 { get; }
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U Property88 { get; }
		U Property89 { get { return default; } }



		static U /* MemberIsNotReadOnly(Field, m_field193, AnalyzedImmutableGenericClassRestrictingT) */ m_field193 /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		static readonly U m_field194 = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		U /* MemberIsNotReadOnly(Field, m_field195, AnalyzedImmutableGenericClassRestrictingT) */ m_field195 /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field196 = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U m_field197 = new U();
		readonly U m_field198 = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field199 = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly U m_field200 = new U();
		U Property90 { get; } = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property91 { get; } = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U Property92 { get; } = new U();



		static Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field201, AnalyzedImmutableGenericClassRestrictingT) */ m_field201 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field202;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field203, AnalyzedImmutableGenericClassRestrictingT) */ m_field203 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field204;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property93 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property94 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field205, AnalyzedImmutableGenericClassRestrictingT) */ m_field205 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field206;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field207, AnalyzedImmutableGenericClassRestrictingT) */ m_field207 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field208;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ Property95 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<U, T> Property96 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field209, AnalyzedImmutableGenericClassRestrictingT) */ m_field209 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field210;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field211, AnalyzedImmutableGenericClassRestrictingT) */ m_field211 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field212;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property97 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property98 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field213, AnalyzedImmutableGenericClassRestrictingT) */ m_field213 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field214;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field215, AnalyzedImmutableGenericClassRestrictingT) */ m_field215 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field216;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ Property99 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<T, U> Property100 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field217, AnalyzedImmutableGenericClassRestrictingT) */ m_field217 /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field218;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field219, AnalyzedImmutableGenericClassRestrictingT) */ m_field219 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field220;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property101 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property102 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field222, AnalyzedImmutableGenericClassRestrictingT) */ m_field222 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field223;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field224, AnalyzedImmutableGenericClassRestrictingT) */ m_field224 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field225;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ Property103 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, U> Property104 { get { return default; } }


		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field226, AnalyzedImmutableGenericClassRestrictingT) */ m_field226 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field227;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field229, AnalyzedImmutableGenericClassRestrictingT) */ m_field229 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field230;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ Property105 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<U, T> Property106 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT<T, U> /* MemberIsNotReadOnly(Field, m_field231, AnalyzedImmutableGenericClassRestrictingT) */ m_field231 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT<T, U> m_field232;
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> /* MemberIsNotReadOnly(Field, m_field233, AnalyzedImmutableGenericClassRestrictingT) */ m_field233 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT<T, U> m_field234;
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> Property107 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> Property108 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field235, AnalyzedImmutableGenericClassRestrictingT) */ m_field235 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field236;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>/* MemberIsNotReadOnly(Field, m_field237, AnalyzedImmutableGenericClassRestrictingT) */ m_field237 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field238;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property109 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property110 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<U, T> /* MemberIsNotReadOnly(Field, m_field238_2, AnalyzedImmutableGenericClassRestrictingT) */ m_field238_2 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, T> m_field239;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> /* MemberIsNotReadOnly(Field, m_field240, AnalyzedImmutableGenericClassRestrictingT) */ m_field240 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, T> m_field241;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> Property111 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> Property112 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field242, AnalyzedImmutableGenericClassRestrictingT) */ m_field242 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field243;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field244, AnalyzedImmutableGenericClassRestrictingT) */ m_field244 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field245;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property113 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property114 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> /* MemberIsNotReadOnly(Field, m_field246, AnalyzedImmutableGenericClassRestrictingT) */ m_field246 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> m_field247;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> /* MemberIsNotReadOnly(Field, m_field248, AnalyzedImmutableGenericClassRestrictingT) */ m_field248 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> m_field249;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> Property115 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> Property116 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field250, AnalyzedImmutableGenericClassRestrictingT) */ m_field250 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field251;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field252, AnalyzedImmutableGenericClassRestrictingT) */ m_field252 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field253;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property117 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property118 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field254, AnalyzedImmutableGenericClassRestrictingT) */ m_field254 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field255;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field256, AnalyzedImmutableGenericClassRestrictingT) */ m_field256 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field257;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property119 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property120 { get { return default; } }

		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		[Mutability.Unaudited(Because.ItsSketchy)]
		object /* ConflictingImmutability(Mutability.Audited, Mutability.Unaudited, field) */ someMutabilityAuditedAndUnauditedObject /**/;

		[Statics.Audited]
		[Statics.Unaudited(Because.ItsSketchy)]
		static object /* ConflictingImmutability(Statics.Audited, Statics.Unaudited, field) */ someStaticsAuditedAndUnauditedObject /**/;

		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* InvalidAuditType(static, field, Statics.*) | MemberIsNotReadOnly(Field, someStaticsMutabilityAuditedObject, AnalyzedImmutableGenericClassRestrictingT) */ someStaticsMutabilityAuditedObject /**/;

		[Statics.Audited]
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* InvalidAuditType(non-static, field, Mutability.*) | MemberIsNotReadOnly(Field, someNonstaticStaticsAuditedObject, AnalyzedImmutableGenericClassRestrictingT) */ someNonstaticStaticsAuditedObject /**/;

		[Statics.Unaudited(Because.ItsSketchy)]
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		[Mutability.Unaudited(Because.ItsSketchy)]
		object /* InvalidAuditType(non-static, field, Mutability.*) | ConflictingImmutability(Mutability.Audited, Mutability.Unaudited, field) */ someNonstaticDoublyAuditedObject /**/;

		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		[Statics.Unaudited(Because.ItsSketchy)]
		static object /* InvalidAuditType(static, field, Statics.*) */ someStaticSortOfDoublyAuditedObject /**/;

		void Method() {
			Types.SomeGenericMethod<T, U>();
			Types.SomeGenericMethod<U, T>();
			Types.SomeGenericMethod<T, T>();
			Types.SomeGenericMethod<U, U>();
			Types.SomeGenericMethodRestrictingT<T, U>();
			Types.SomeGenericMethodRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>();
			Types.SomeGenericMethodRestrictingT<T, T>();
			Types.SomeGenericMethodRestrictingT </* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, U>();
			Types.SomeGenericMethodRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingU<U, T>();
			Types.SomeGenericMethodRestrictingU<T, T>();
			Types.SomeGenericMethodRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>();
			Types.SomeGenericMethodRestrictingTU<T, T>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			object foo = new object();
			int bar = 529;
			Types.SomeGenericMethodRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/>(foo);
			Types./* NonImmutableTypeHeldByImmutable(class, object, ) */ SomeGenericMethodRestrictingT /**/(foo);
			Types.SomeGenericMethodRestrictingT<int>(bar);
			Types.SomeGenericMethodRestrictingT(bar);
		}
	}

	[ConditionallyImmutable]
	public sealed class AnalyzedImmutableGenericClassGivenT<[ConditionallyImmutable.OnlyIf] T, U>
		where T : new()
		where U : new()
	{



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/ /* MemberIsNotReadOnly(Field, m_field258, AnalyzedImmutableGenericClassGivenT) */ m_field258 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/ m_field259;
		T /* MemberIsNotReadOnly(Field, m_field260, AnalyzedImmutableGenericClassGivenT) */ m_field260 /**/;
		readonly T m_field261;
		T Property121 { get; }
		T Property122 { get { return default; } }



		static T /* MemberIsNotReadOnly(Field, m_field262, AnalyzedImmutableGenericClassGivenT) */ m_field262 /**/ = /* TypeParameterIsNotKnownToBeImmutable(T) */ new T() /**/;
		static readonly T m_field263 = /* TypeParameterIsNotKnownToBeImmutable(T) */ new T() /**/;
		T /* MemberIsNotReadOnly(Field, m_field265, AnalyzedImmutableGenericClassGivenT) */ m_field265 /**/ = new T();
		readonly T m_field266 = new T();
		T Property123 { get; } = new T();
		T Property124 { get { return new T(); } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field267, AnalyzedImmutableGenericClassGivenT) */ m_field267 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field268;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field269, AnalyzedImmutableGenericClassGivenT) */ m_field269 /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field270;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U m_field271;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field272;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field273;
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly U m_field274;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ Property125 { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property126 { get; }
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U Property127 { get; }
		U Property128 { get { return default; } }



		static U /* MemberIsNotReadOnly(Field, m_field275, AnalyzedImmutableGenericClassGivenT) */ m_field275 /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		static readonly U m_field276 = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		U /* MemberIsNotReadOnly(Field, m_field278, AnalyzedImmutableGenericClassGivenT) */ m_field278 /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field279 = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U m_field280 = new U();
		readonly U m_field281 = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field282 = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		readonly U m_field283 = new U();
		U Property129 { get; } = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property130 { get; } = new U();
		[Mutability.Audited( "John Doe", "1970-01-01", "Rationale" )]
		U Property131 { get; } = new U();



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenT<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field284, AnalyzedImmutableGenericClassGivenT) */ m_field284 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenT<T, U> /**/ m_field285;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field287, AnalyzedImmutableGenericClassGivenT) */ m_field287 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field288;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property132 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property133 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field289, AnalyzedImmutableGenericClassGivenT) */ m_field289 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field290;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field292, AnalyzedImmutableGenericClassGivenT) */ m_field292 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field293;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ Property134 { get; }
		Types.SomeImmutableGenericInterfaceGivenT<U, T> Property135 { get { return default; } }


		static  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field294, AnalyzedImmutableGenericClassGivenT) */ m_field294 /**/;
		static readonly  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenU<U, T> /**/ m_field295;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field297, AnalyzedImmutableGenericClassGivenT) */ m_field297 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field298;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property136 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property137 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field299, AnalyzedImmutableGenericClassGivenT) */ m_field299 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field300;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field301, AnalyzedImmutableGenericClassGivenT) */ m_field301 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field302;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ Property138 { get; }
		Types.SomeImmutableGenericInterfaceGivenU<T, U> Property139 { get { return default; } }



		static  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, T> /**/ /* MemberIsNotReadOnly(Field, m_field303, AnalyzedImmutableGenericClassGivenT) */ m_field303 /**/;
		static readonly  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, T> /**/ m_field304;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field305, AnalyzedImmutableGenericClassGivenT) */ m_field305 /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field306;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property140 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property141 { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field307, AnalyzedImmutableGenericClassGivenT) */ m_field307 /**/;
		static readonly /*  TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field308;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field310, AnalyzedImmutableGenericClassGivenT) */ m_field310 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field311;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ Property142 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, U> Property143 { get { return default; } }


		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field312, AnalyzedImmutableGenericClassGivenT) */ m_field312 /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field313;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field314, AnalyzedImmutableGenericClassGivenT) */ m_field314 /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field315;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ Property144 { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<U, T> Property145 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> /* MemberIsNotReadOnly(Field, m_field316, AnalyzedImmutableGenericClassGivenT) */ m_field316 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> m_field317;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> /* MemberIsNotReadOnly(Field, m_field318, AnalyzedImmutableGenericClassGivenT) */ m_field318 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> m_field319;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> Property146 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> Property147 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field320, AnalyzedImmutableGenericClassGivenT) */ m_field320 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field321;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>/* MemberIsNotReadOnly(Field, m_field323, AnalyzedImmutableGenericClassGivenT) */ m_field323 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field324;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property148 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property149 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field325, AnalyzedImmutableGenericClassGivenT) */ m_field325 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field326;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field328, AnalyzedImmutableGenericClassGivenT) */ m_field328 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field329;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property150 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property151 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field331, AnalyzedImmutableGenericClassGivenT) */ m_field331 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field332;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field334, AnalyzedImmutableGenericClassGivenT) */ m_field334 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field335;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property152 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property153 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field336, AnalyzedImmutableGenericClassGivenT) */ m_field336 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field337;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field338, AnalyzedImmutableGenericClassGivenT) */ m_field338 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field339;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property154 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property155 { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field340, AnalyzedImmutableGenericClassGivenT) */ m_field340 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field341;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field342, AnalyzedImmutableGenericClassGivenT) */ m_field342 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field343;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property156 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property157 { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field344, AnalyzedImmutableGenericClassGivenT) */ m_field344 /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field345;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field346, AnalyzedImmutableGenericClassGivenT) */ m_field346 /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field347;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property158 { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property159 { get { return default; } }

		// These are MethodDeclarationSyntax
		void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/>() { }
		void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, U>() { }
		void SomeGenericMethodConditionallyRestrictingU<T, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }
		void SomeGenericMethodConditionallyRestrictingTU</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }

		sealed class SomeGenericClassDoublyRestrictingT<[Immutable] [ConditionallyImmutable.OnlyIf] /* ConflictingImmutability(Immutable, ConditionallyImmutable.OnlyIf, typeparameter) */ T /**/> { }
		sealed class SomeGenericClassRestrictingT<[Immutable] T> { }
		sealed class SomeGenericClassConditionallyRestrictingT<[ConditionallyImmutable.OnlyIf] T> { }

		void Method()
		{
			// These are LocalFunctionStatementSyntax
			void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/>() { }
			void SomeGenericMethodConditionallyRestrictingT2</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, U>() { }
			void SomeGenericMethodConditionallyRestrictingU<T, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }
			void SomeGenericMethodConditionallyRestrictingTU</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }
			Types.SomeGenericMethod<T, U>();
			Types.SomeGenericMethod<U, T>();
			Types.SomeGenericMethod<T, T>();
			Types.SomeGenericMethod<U, U>();
			Types.SomeGenericMethodRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U>();
			Types.SomeGenericMethodRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>();
			Types.SomeGenericMethodRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, T>();
			Types.SomeGenericMethodRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, U>();
			Types.SomeGenericMethodRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/>();
			Types.SomeGenericMethodRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/>();
			Types.SomeGenericMethodRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/>();
			Types.SomeGenericMethodRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>();
			object foo = new object();
			int bar = 529;
			Types.SomeGenericMethodRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/>(foo);
			Types./* NonImmutableTypeHeldByImmutable(class, object, ) */ SomeGenericMethodRestrictingT /**/(foo);
			Types.SomeGenericMethodRestrictingT<int>(bar);
			Types.SomeGenericMethodRestrictingT(bar);
		}
	}

	public static class StaticAudits {
		[Statics.Audited]
		static int staticint1;

		[Statics.Audited]
		static readonly object staticreadonlyobject1;

		[/* UnnecessaryMutabilityAnnotation */ Statics.Audited /**/]
		static readonly object m_lock1 = new object();

		[Statics.Unaudited( Because.ItHasntBeenLookedAt )]
		static int staticint2;

		[Statics.Unaudited( Because.ItHasntBeenLookedAt )]
		static readonly object staticreadonlyobject2;

		[/* UnnecessaryMutabilityAnnotation */ Statics.Unaudited( Because.ItHasntBeenLookedAt ) /**/]
		static readonly object m_lock = new object();
    }

	[Immutable]
	public record SomeRecord {
		SomeRecord V { get; }

		int W { get; }
		Types.SomeImmutableClass X { get; }

		int /* MemberIsNotReadOnly(Property, Y, SomeRecord) */ Y /**/ { get; set; }

		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ Z { get; }

		public SomeRecord( SomeRecord v, int w, Types.SomeImmutableClass x, int y, object z )
			=> /* UnknownImmutabilityAssignmentKind(Deconstructed assignment) */ (V, W, X, Y, Z) /**/ = (v, w, x, y, z);
    }

	record NonImmutableBaseRecord(object x);

	[Immutable]
	record DerivedRecordWithQuestionableBase :
		/* NonImmutableTypeHeldByImmutable(class, SpecTests.NonImmutableBaseRecord,  (or [ImmutableBaseClass])) */ NonImmutableBaseRecord /**/
	{
	  public DerivedRecordWithQuestionableBase() : base( new object() ) {}
	}

	[ImmutableBaseClass]
	record BaseRecordWithImmutableBaseClass { }

	[Immutable]
	record DerivedFromImmutableBaseClassRecord : BaseRecordWithImmutableBaseClass {
		/* NonImmutableTypeHeldByImmutable(class, SpecTests.BaseRecordWithImmutableBaseClass, ) */ BaseRecordWithImmutableBaseClass /**/ X { get; }
    }

	[Immutable]
	record ConciseRecord(
		int xxxx,
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ x,
		/* NonImmutableTypeHeldByImmutable(class, SpecTests.BaseRecordWithImmutableBaseClass, ) */ BaseRecordWithImmutableBaseClass /**/ y,
		ConciseRecord z
	);
}

namespace ConsistencyTests {

	public interface IVanilla { }
	public interface IVanilla2 { }
	public class VanillaBase : IVanilla { }
	public class VanillaDerived : VanillaBase { }
	public sealed class VanillaDerived2 : VanillaDerived, IVanilla2 { }
	public struct VanillaStruct : IVanilla, IVanilla2 { }

	[Immutable]
	public interface ISomethingImmutable { }

	[Immutable]
	public class HappyImplementor : ISomethingImmutable { }

	[Immutable]
	public sealed class HappyDeriver : HappyImplementor { }

	[Immutable]
	public struct HappyStructImplementor : ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadImplementor, , interface, ConsistencyTests.ISomethingImmutable) */ SadImplementor /**/
		: ISomethingImmutable { }

	public struct
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadStructImplementor, , interface, ConsistencyTests.ISomethingImmutable) */ SadStructImplementor /**/
		: ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadDeriver, , base class, ConsistencyTests.HappyImplementor) */ SadDeriver /**/
		: HappyImplementor { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadImplementor2, , interface, ConsistencyTests.ISomethingImmutable) */ SadImplementor2 /**/
		: VanillaBase, IVanilla, IVanilla2, ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadImplementor3, , base class, ConsistencyTests.HappyImplementor) */ SadImplementor3 /**/
		: HappyImplementor, IVanilla, IVanilla2 { }

	public interface
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.SadExtender, , interface, ConsistencyTests.ISomethingImmutable) */ SadExtender /**/
		: ISomethingImmutable { }

	// This won't emit an error because SadDeriver hasn't added [Immutable].
	// There is a diagnostic for that mistake, but once its fixed we would
	// get a diagnostic here. It would be nicer to developers to report all
	// the violations, probably, but this keeps the implementation very simple
	// and it's unlikely to come up in practice if you make small changes
	// between compiles.
	public sealed class IndirectlySadClass : SadDeriver { }

	public partial class
	/* MissingTransitiveImmutableAttribute(ConsistencyTests.PartialClass, , interface, ConsistencyTests.ISomethingImmutable) */ PartialClass /**/
		: ISomethingImmutable { }

	// This one doesn't get the diagnostic. We attach it to the one that specified
	// the interface.
	public partial class PartialClass { }

	// We will emit another diagnostic here though... this makes sense but the
	// code fix will try to apply multiple [Immutable] attributes which isn't
	// allowed...
	public partial class
	/* MissingTransitiveImmutableAttribute(ConsistencyTests.PartialClass, , base class, ConsistencyTests.HappyImplementor) */ PartialClass /**/
		: HappyImplementor { }

	// This shouldn't crash the analyzer
	public sealed class Foo : IThingThatDoesntExist { }

	[Immutable]
	public record UnsealedImmutableRecord { }

	public sealed record
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.DerivedRecordMissingAttribute, , base class, ConsistencyTests.UnsealedImmutableRecord) */ DerivedRecordMissingAttribute /**/
		: UnsealedImmutableRecord { }

	[Immutable]
	public sealed record SealedDerivedWithAttribute : UnsealedImmutableRecord { }

	[Immutable]
	public record UnsealedDerivedWithAttribute : UnsealedImmutableRecord { }

	public record RegularRecord { }
	public record class RegularExplicitRecord { }
	public sealed record RegularDerivedRecord : RegularRecord { }
	public sealed record class RegularDerivedExplicitRecord : RegularRecord { }

	[Immutable]
	public record ConciseRecord : UnsealedImmutableRecord;

	[Immutable]
	public record class ConsiseExplicitRecord : UnsealedImmutableRecord;

	[Immutable]
	public record BaseRecordWithArgs( int x ) { }

	[Immutable]
	public record ImmutableDerivedWithArgs( int y ) : BaseRecordWithArgs( y );

	public sealed record
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.DerivedRecordNoAttrConstArg, , base class, ConsistencyTests.BaseRecordWithArgs) */ DerivedRecordNoAttrConstArg /**/
		: BaseRecordWithArgs( 0 );

	public sealed record
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.DerivedRecordNoAttrWithArg, , base class, ConsistencyTests.BaseRecordWithArgs) */ DerivedRecordNoAttrWithArg /**/
		( int z ) : BaseRecordWithArgs( z );

	public sealed record class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.DerivedExplicitRecordNoAttrConstArg, , base class, ConsistencyTests.BaseRecordWithArgs) */ DerivedExplicitRecordNoAttrConstArg /**/
		: BaseRecordWithArgs( 0 );

	public sealed record class
		/* MissingTransitiveImmutableAttribute(ConsistencyTests.DerivedExplicitRecordNoAttrWithArg, , base class, ConsistencyTests.BaseRecordWithArgs) */ DerivedExplicitRecordNoAttrWithArg /**/
		( int z ) : BaseRecordWithArgs( z );


	[Immutable]
	public readonly record struct ReadOnlyRecordStruct() { }

	[Immutable]
	public readonly record struct ReadOnlyRecordStructWithArg( int x ) { }

	[Immutable]
	public record struct RecordStruct() { }

	[Immutable]
	public record struct RecordStructWithArg(
		int /* MemberIsNotReadOnly(Property, x, RecordStructWithArg) */ x /**/
	) { }

	[Immutable]
	public record struct RecordStructWithExplicitReadOnlyArgImpl(
		int x
	) {
		public int x { get; init; }
	}

	[ConditionallyImmutable]
	public interface ISomethingConditionallyImmutable<[ConditionallyImmutable.OnlyIf] T> { }

	[Immutable]
	public sealed class ImmutableClassImplementingConditionallyImmutable<[Immutable] T> : ISomethingConditionallyImmutable<T> { }

	[ConditionallyImmutable]
	public sealed class ConditionallyImmutableClassImplementingConditionallyImmutable<[ConditionallyImmutable.OnlyIf] T> : ISomethingConditionallyImmutable<T> { }

	public sealed class /* MissingTransitiveImmutableAttribute(ConsistencyTests.SadImplementerOfConditionallyImmutable,  (or [ConditionallyImmutable]), interface, ConsistencyTests.ISomethingConditionallyImmutable) */ SadImplementerOfConditionallyImmutable /**/<T> : ISomethingConditionallyImmutable<T> { }

	public partial class PartialClassNeedingImmutable { }
	public partial class /* MissingTransitiveImmutableAttribute(ConsistencyTests.PartialClassNeedingImmutable, , interface, ConsistencyTests.ISomethingImmutable) */ PartialClassNeedingImmutable /**/ : ISomethingImmutable { }
	public partial class PartialClassNeedingImmutable { }

	public class ClassWithMethod {
		public abstract void MethodWithImmutable<[Immutable] T>();
	}
	public interface IInterfaceWithMethodA {
		void MethodWithImmutable<T, [Immutable] U>();
	}
	public interface IInterfaceWithMethodB {
		void MethodWithImmutable<[Immutable] T, U>();
	}

	public class ClassImplicitlyImplementingMethodsA : IInterfaceWithMethodA {
		public void MethodWithImmutable<
			T,
			[Immutable] U
		>() { }
	}
	public class ClassImplicitlyImplementingMethodsB : IInterfaceWithMethodB {
		public void MethodWithImmutable<
			[Immutable] T,
			U
		>() { }
	}
	public class ClassImplicitlyImplementingMethodsInconsistent : IInterfaceWithMethodA, IInterfaceWithMethodB {
		public void MethodWithImmutable<
			/* InconsistentMethodAttributeApplication(Immutable, ClassImplicitlyImplementingMethodsInconsistent.MethodWithImmutable, IInterfaceWithMethodA.MethodWithImmutable) */ [Immutable] T /**/,
			/* InconsistentMethodAttributeApplication(Immutable, ClassImplicitlyImplementingMethodsInconsistent.MethodWithImmutable, IInterfaceWithMethodA.MethodWithImmutable) */ U /**/
		>() {}
	}

	public class ClassOverridingMethods : ClassWithMethod {
		public override void MethodWithImmutable<
			[Immutable] T
		>() { }
	}
	public class ClassOverridingMethodsInconsistent : ClassWithMethod {
		public override void MethodWithImmutable<
			/* InconsistentMethodAttributeApplication(Immutable, ClassOverridingMethodsInconsistent.MethodWithImmutable, ClassWithMethod.MethodWithImmutable) */ T /**/
		>() { }
	}

	public class ClassExplicitlyImplementingMethods : IInterfaceWithMethodA, IInterfaceWithMethodB {
		void IInterfaceWithMethodA.MethodWithImmutable<
			T,
			[Immutable] U
		>() { }
		void IInterfaceWithMethodB.MethodWithImmutable<
			[Immutable] T,
			U
		>()
	}
	public class ClassExplicitlyImplementingMethodsInconsistent : IInterfaceWithMethodA, IInterfaceWithMethodB {
		void IInterfaceWithMethodA.MethodWithImmutable<
			/* InconsistentMethodAttributeApplication(Immutable, ClassExplicitlyImplementingMethodsInconsistent.ConsistencyTests.IInterfaceWithMethodA.MethodWithImmutable, IInterfaceWithMethodA.MethodWithImmutable) */ [Immutable] T /**/,
			/* InconsistentMethodAttributeApplication(Immutable, ClassExplicitlyImplementingMethodsInconsistent.ConsistencyTests.IInterfaceWithMethodA.MethodWithImmutable, IInterfaceWithMethodA.MethodWithImmutable) */ U /**/
		>() { }
		void IInterfaceWithMethodB.MethodWithImmutable<
			/* InconsistentMethodAttributeApplication(Immutable, ClassExplicitlyImplementingMethodsInconsistent.ConsistencyTests.IInterfaceWithMethodB.MethodWithImmutable, IInterfaceWithMethodB.MethodWithImmutable) */ T /**/,
			/* InconsistentMethodAttributeApplication(Immutable, ClassExplicitlyImplementingMethodsInconsistent.ConsistencyTests.IInterfaceWithMethodB.MethodWithImmutable, IInterfaceWithMethodB.MethodWithImmutable) */ [Immutable] U /**/
		>()
	}

	[Immutable]
	public sealed class ClassWithCapturedMutability {
		public static readonly Func<int> AlwaysOk = static () => 3;
		public static readonly Func<int> AlwaysOk2 = () => AlwaysOk() + AlwaysOk();
		public static readonly Func<int> SometimesBad = () => AlwaysOk2();

		// won't error, but its cool because we'll error for SometimesBad
		public readonly Func<int> SometimesBad2 = () => SometimesBad()*3; 

		static ClassWithCapturedMutability() {
			int x = 0;

			AlwaysOk = static () => 3;

			// won't error, but its cool because we'll error for SometimesBad.
			AlwaysOk2 = static () => SometimesBad();

			SometimesBad = /* AnonymousFunctionsMayCaptureMutability */ () => x-- /**/;
        }

		ClassWithCapturedMutability() {
			int x = 0;

			SometimesBad2 = static () => 4;
			SometimesBad2 = static () => AlwaysOk();
			SometimesBad2 = /* AnonymousFunctionsMayCaptureMutability */ () => x++ /**/;
			var x = SometimesBad2 = /* AnonymousFunctionsMayCaptureMutability */ () => x++ /**/;
			SometimesBad2 = static () => 2;
        }
    }
}
