// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using System.ComponentModel;
using D2L.CodeStyle.Annotations;

#region Relevant Types

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute {}
		public sealed class Immutable : ImmutableAttributeBase { }
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }
		public sealed class ConditionallyImmutable : ImmutableAttributeBase {
			public sealed class OnlyIf : ImmutableAttributeBase { }
		}
	}
	public static class Mutability {
		public sealed class AuditedAttribute : Attribute { }
		public sealed class UnauditedAttribute : Attribute {
			public UnauditedAttribute( Because why ) { }
		}
	}
	public static class Statics {
		public sealed class Audited : Attribute { }
		public sealed class Unaudited : Attribute {
			public Unaudited( Because why ) { }
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

	using static Objects;

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

			public SomeClassWithConstructor5() {
				m_int = 29;
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
		#endregion


		public class RegularClass {
			private const int m_const = 0;

			private int m_field = 0;

			private readonly int m_field = 0;

		}
		public class RegularExtension : RegularClass { }
		public sealed class RegularSealedExtension : RegularClass { }

		public class SomeMutableClassWithImmutableTypeParameter<[Immutable] T> { }

		[ImmutableBaseClass]
		public class SomeImmutableBaseClass { }
		static SomeImmutableBaseClass FuncReturningSomeImmutableBaseClass() => null;
		public sealed class MutableExtensionOfSomeImmutableBaseClass : SomeImmutableBaseClass { }

		[ImmutableBaseClass]
		public class SomeImmutableGenericBaseClassRestrictingT<[Immutable] T> { }

		[Immutable]
		public class SomeImmutableClass { }
		static SomeImmutableClass FuncReturningSomeImmutableClass() => null;

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

	public sealed class NonImmutableClassWithNonImmutableTypeParameterImplementingImmutableBaseClassParameter<T> : Types.SomeImmutableGenericBaseClassRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> { }
	public sealed class NonImmutableClassWithNonImmutableTypeParameterImplementingImmutableBaseClassParameter<T> : Types.SomeMutableClassWithImmutableTypeParameter</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> { }

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
	public sealed interface /* ConflictingImmutability(Immutable, ConditionallyImmutable, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities1 /**/ { }

	[Immutable]
	[ImmutableBaseClassAttribute]
	public sealed interface /* ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities2 /**/ { }

	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed interface /* ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities3 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed interface /* ConflictingImmutability(Immutable, ConditionallyImmutable, interface)
					         | ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, interface)
	                         | ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, interface) */ AnalyzedInterfaceMarkedWithMultipleImmutabilities4 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	public sealed struct /* ConflictingImmutability(Immutable, ConditionallyImmutable, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities1 /**/ { }

	[Immutable]
	[ImmutableBaseClassAttribute]
	public sealed struct /* ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities2 /**/ { }

	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed struct /* ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities3 /**/ { }

	[Immutable]
	[ConditionallyImmutable]
	[ImmutableBaseClassAttribute]
	public sealed struct /* ConflictingImmutability(Immutable, ConditionallyImmutable, struct)
	                      | ConflictingImmutability(Immutable, ImmutableBaseClassAttribute, struct)
	                      | ConflictingImmutability(ConditionallyImmutable, ImmutableBaseClassAttribute, struct) */ AnalyzedStructMarkedWithMultipleImmutabilities4 /**/	{ }

	[Immutable]
	public sealed class AnalyzedClassMarkedImmutable {



		class SomeEventArgs { }
		delegate void SomeEventHandler( object sender, SomeEventArgs e );
		/* EventMemberMutable() */ event SomeEventHandler SomeEvent; /**/


		object this[ int index ] {
			get { return null };
			set { return; }
		}

		public int X { get; init; }

		// This isn't safe because init-only properties can be overwritten by
		// any caller (which we may not be able to analyze).
		public /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ Y { get; init; } = new object();

		static int /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = 0;
		static readonly int m_field = 0;
		int /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = 0;
		int /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int m_field = 0;
		[Mutability.Audited]
		int m_field = 0;
		readonly int m_field = 0;
		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Unaudited( Because.ItHasntBeenLookedAt ) /**/]
		readonly int m_field = 0;
		[/* UnnecessaryMutabilityAnnotation() */ Mutability.Audited /**/]
		readonly int m_field = 0;
		readonly int m_field;
		int Property { get; } = 0;
		int Property { get; }
		int Property { get { return 0; } }

		// target-typed new initializers
		readonly object m_lock = new();


		static Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = Types.SomeEnum.Foo;
		static readonly Types.SomeEnum m_field = Types.SomeEnum.Foo;
		Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = Types.SomeEnum.Foo;
		Types.SomeEnum /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.SomeEnum m_field = Types.SomeEnum.Foo;
		[Mutability.Audited]
		Types.SomeEnum m_field = Types.SomeEnum.Foo;
		readonly Types.SomeEnum m_field = Types.SomeEnum.Foo;
		readonly Types.SomeEnum m_field;
		Types.SomeEnum Property { get; } = Types.SomeEnum.Foo;
		Types.SomeEnum Property { get; }
		Types.SomeEnum Property { get { return Types.SomeEnum.Foo; } }



		static int[] /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		static readonly int[] m_field = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		int[] /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		/* ArraysAreMutable(Int32) */ int[] /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int[] m_field = new[] { 0 };
		[Mutability.Audited]
		int[] m_field = new[] { 0 };
		readonly int[] m_field = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		readonly /* ArraysAreMutable(Int32) */ int[] /**/ m_field;
		int[] Property { get; } = /* ArraysAreMutable(Int32) */ new[] { 0 } /**/;
		/* ArraysAreMutable(Int32) */ int[] /**/ Property { get; }
		int[] Property { get { return new[] { 0 }; } }



		static readonly /* UnexpectedTypeKind(PointerType) */ int* /**/ m_field;
		/* UnexpectedTypeKind(PointerType) */ int* /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		int* m_field;
		[Mutability.Audited]
		int* m_field;
		readonly /* UnexpectedTypeKind(PointerType) */ int* /**/ m_field;
		/* UnexpectedTypeKind(PointerType) */ int* /**/ Property { get; }
		int* Property { get { return new[] { 0 }; } }



		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ Property { get; }


		static object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = null;
		static readonly object m_field = null;
		object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = null;
		readonly object m_field = null;
		object Property { get; } = null;
		object Property { get { return null; } }



		static object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new object();
		static readonly object m_field = new object();
		object /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new object();
		readonly object m_field = new object();
		object Property { get; } = new object();
		object Property { get { return new object(); } }



		static readonly /* DynamicObjectsAreMutable */ dynamic /**/ m_field;
		/* DynamicObjectsAreMutable */ dynamic /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* DynamicObjectsAreMutable */ dynamic /**/ m_field;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly dynamic m_field;
		[Mutability.Audited]
		readonly dynamic m_field;
		/* DynamicObjectsAreMutable */ dynamic /**/ Property { get; };
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		dynamic Property { get; }
		[Mutability.Audited]
		dynamic Property { get; }
		dynamic Property { get { return new ExpandoObject(); } }



		static Func<object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = () => null;
		static readonly Func<object> m_field = () => null;
		Func<object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = () => null;
		readonly Func<object> m_field = () => null;
		Func<object> Property { get; } = () => null;
		Func<object> Property { get { return () => null; } }



		static Func<object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = () => { return null };
		static readonly Func<object> m_field = () => { return null };
		Func<object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = () => { return null };
		readonly Func<object> m_field = () => { return null };
		Func<object> Property { get; } = () => { return null };
		Func<object> Property { get { return () => { return null }; } }



		static (int, int) /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly (int, int) m_field;
		(int, int) /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly (int, int) m_field;
		(int, int) Property { get; }
		(int, int) Property { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (object, int) /**/ Property { get; }
		(object, int) Property { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly  /* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ (int, object) /**/ Property { get; }
		(int, object) Property { get { return default; } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		static readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.RegularClass m_field = new Types.RegularClass ();
		[Mutability.Audited]
		readonly Types.RegularClass m_field = new Types.RegularClass();
		Types.RegularClass Property { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularClass,  (or [ImmutableBaseClass])) */ new Types.RegularClass() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.RegularClass Property { get; } = new Types.RegularClass ();
		[Mutability.Audited]
		Types.RegularClass Property { get; } = new Types.RegularClass();
		Types.RegularClass Property { get { return new Types.RegularClass(); } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		static readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass Property { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularExtension,  (or [ImmutableBaseClass])) */ new Types.RegularExtension() /**/;
		Types.RegularClass Property { get { return new Types.RegularExtension(); } }



		static Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		static readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		readonly Types.RegularClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass Property { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.RegularSealedExtension, ) */ new Types.RegularSealedExtension() /**/;
		Types.RegularClass Property { get { return new Types.RegularSealedExtension(); } }



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.SomeImmutableBaseClass();
		readonly Types.SomeImmutableBaseClass m_field = new Types.SomeImmutableBaseClass();
		Types.SomeImmutableBaseClass Property { get; } = new Types.SomeImmutableBaseClass();



		readonly Types.SomeImmutableBaseClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;
		Types.SomeImmutableBaseClass Property { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.SomeImmutableBaseClass, ) */ Types.FuncReturningSomeImmutableBaseClass() /**/;



		Types.SomeImmutableBaseClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;
		readonly Types.SomeImmutableBaseClass m_field = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;
		Types.SomeImmutableBaseClass Property { get; } = /* NonImmutableTypeHeldByImmutable(class, SpecTests.Types.MutableExtensionOfSomeImmutableBaseClass, ) */ new Types.MutableExtensionOfSomeImmutableBaseClass() /**/;



		Types.SomeImmutableClass /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.SomeImmutableClass();
		readonly Types.SomeImmutableClass m_field = new Types.SomeImmutableClass();
		Types.SomeImmutableClass Property { get; } = new Types.SomeImmutableClass();



		readonly Types.SomeImmutableClass m_field = Types.FuncReturningSomeImmutableClass();
		Types.SomeImmutableClass Property { get; } = Types.FuncReturningSomeImmutableClass();



		static /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ m_field;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.RegularInterface m_field;
		[Mutability.Audited]
		readonly Types.RegularInterface m_field;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.RegularInterface, ) */ Types.RegularInterface /**/ Property { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.RegularInterface Property { get; }
		[Mutability.Audited]
		Types.RegularInterface Property { get; }



		static Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();
		static readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableImplementingRegularInterface();
		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableImplementingRegularInterface();
		readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableImplementingRegularInterface();
		Types.RegularInterface Property { get; } = new Types.ClassMarkedImmutableImplementingRegularInterface();



		static Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		static readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		Types.RegularInterface /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		readonly Types.RegularInterface m_field = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();
		Types.RegularInterface Property { get; } = new Types.ClassMarkedImmutableBaseClassImplementingRegularInterface();



		static /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		Types.SomeStruct /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/ = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		readonly /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ m_field;
		readonly Types.SomeStruct  m_field = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly Types.SomeStruct m_field;
		[Mutability.Audited]
		readonly Types.SomeStruct m_field;
		/* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ Types.SomeStruct /**/ Property { get; }
		Types.SomeStruct Property { get; } = /* NonImmutableTypeHeldByImmutable(structure, SpecTests.Types.SomeStruct, ) */ new Types.SomeStruct() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		Types.SomeStruct Property { get; }
		[Mutability.Audited]
		Types.SomeStruct Property { get; }



		static Types.SomeImmutableStruct /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableStruct m_field;
		Types.SomeImmutableStruct /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableStruct m_field;
		Types.SomeImmutableStruct Property { get; }



		static /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(interface, SpecTests.Types.SomeGenericInterface<int\, int>, ) */ Types.SomeGenericInterface<int, int> /**/ Property { get; }
		Types.SomeGenericInterface<int, int> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenT<int, object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenT<int, object> m_field;
		Types.SomeImmutableGenericInterfaceGivenT<int, object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<int, object> m_field;
		Types.SomeImmutableGenericInterfaceGivenT<int, object> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<int, object> Property { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenT<object, int> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<object, int> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceGivenU<object, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenU<object, int> m_field;
		Types.SomeImmutableGenericInterfaceGivenU<object, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<object, int> m_field;
		Types.SomeImmutableGenericInterfaceGivenU<object, int> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<object, int> Property { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenU<int, object> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<int, object> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenTU<int, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenTU<int, int> m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<int, int> m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<int, int> Property { get { return default; } }



		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<int, object> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<int, object> Property { get { return default; } }


		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly /* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ m_field;
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ Types.SomeImmutableGenericInterfaceGivenTU<object, int> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<object, int> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT<int, object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT<int, object> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT<int, object> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT<int, object> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<object, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<object, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<object, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<object, int> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, int> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<int, /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> /* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int>/* MemberIsNotReadOnly(Field, m_field, AnalyzedClassMarkedImmutable) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int>m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/, int> Property { get { return default; } }
	}

	[Immutable]
	public sealed class AnalyzedImmutableGenericClassRestrictingT<[Immutable] T, U> {



		static T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly T m_field;
		T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly T m_field;
		T Property { get; }
		T Property { get { return default; } }



		static T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/ = new T();
		static readonly T m_field = new T();
		T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/ = new T();
		readonly T m_field = new T();
		T Property { get; } = new T()
		T Property { get { return new T(); } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field;
		[Mutability.Audited]
		U m_field;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field;
		[Mutability.Audited]
		readonly U m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ Property { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property { get; }
		[Mutability.Audited]
		U Property { get; }
		U Property { get { return default; } }



		static U /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		static readonly U m_field = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		U /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field = new U();
		[Mutability.Audited]
		U m_field = new U();
		readonly U m_field = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field = new U();
		[Mutability.Audited]
		readonly U m_field = new U();
		U Property { get; } = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property { get; } = new U();
		[Mutability.Audited]
		U Property { get; } = new U();



		static Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<U, T> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<T, U> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, U> Property { get { return default; } }


		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<U, T> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT<T, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT<T, U> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT<T, U> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT<T, U> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>/* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<U, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<U, T> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, T> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassRestrictingT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get { return default; } }

		[Mutability.Audited("Timothy J Cowen", "2020-12-16", "Actually this shouldn't work.")]
		[Mutability.Unaudited(Because.ItsSketchy)]
		object /* ConflictingImmutability(Mutability.Audited, Mutability.Unaudited, field) */ someMutabilityAuditedAndUnauditedObject /**/;

		[Statics.Audited("Timothy J Cowen", "2020-12-16", "This shouldn't work either...")]
		[Statics.Unaudited(Because.ItsSketchy)]
		static object /* ConflictingImmutability(Statics.Audited, Statics.Unaudited, field) */ someStaticsAuditedAndUnauditedObject /**/;

		[Mutability.Audited("Timothy J Cowen", "2020-12-16", "This also shouldn't either......")]
		static /* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* InvalidAuditType(static, field, Statics.*) | MemberIsNotReadOnly(Field, someStaticsMutabilityAuditedObject, AnalyzedImmutableGenericClassRestrictingT) */ someStaticsMutabilityAuditedObject /**/;

		[Statics.Audited("Timothy J Cowen", "2020-12-16", "Nothing works.........")]
		/* NonImmutableTypeHeldByImmutable(class, object, ) */ object /**/ /* InvalidAuditType(non-static, field, Mutability.*) | MemberIsNotReadOnly(Field, someNonstaticStaticsAuditedObject, AnalyzedImmutableGenericClassRestrictingT) */ someNonstaticStaticsAuditedObject /**/;

		[Statics.Unaudited(Because.ItsSketchy)]
		[Mutability.Audited("Timothy J Cowen", "2020-12-16", "Seriously............?")]
		[Mutability.Unaudited(Because.ItsSketchy)]
		object /* InvalidAuditType(non-static, field, Mutability.*) | ConflictingImmutability(Mutability.Audited, Mutability.Unaudited, field) */ someNonstaticDoublyAuditedObject /**/;

		[Mutability.Audited("Timothy J Cowen", "2020-12-16", "I give up.")]
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
	public sealed class AnalyzedImmutableGenericClassGivenT<[ConditionallyImmutable.OnlyIf] T, U> {



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/ m_field;
		T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly T m_field;
		T Property { get; }
		T Property { get { return default; } }



		static T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/ = /* TypeParameterIsNotKnownToBeImmutable(T) */ new T() /**/;
		static readonly T m_field = /* TypeParameterIsNotKnownToBeImmutable(T) */ new T() /**/;
		T /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/ = new T();
		readonly T m_field = new T();
		T Property { get; } = new T()
		T Property { get { return new T(); } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field;
		[Mutability.Audited]
		U m_field;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ m_field;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field;
		[Mutability.Audited]
		readonly U m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/ Property { get; }
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property { get; }
		[Mutability.Audited]
		U Property { get; }
		U Property { get { return default; } }



		static U /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		static readonly U m_field = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		U /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/ = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U m_field = new U();
		[Mutability.Audited]
		U m_field = new U();
		readonly U m_field = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		readonly U m_field = new U();
		[Mutability.Audited]
		readonly U m_field = new U();
		U Property { get; } = /* TypeParameterIsNotKnownToBeImmutable(U) */ new U() /**/;
		[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
		U Property { get; } = new U();
		[Mutability.Audited]
		U Property { get; } = new U();



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenT<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenT<T, U> /**/ m_field;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenT<T, U> m_field;
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<T, U> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenT<U, T> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenT<U, T> Property { get { return default; } }


		static  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenU<U, T> /**/ m_field;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenU<U, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<U, T> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenU<T, U> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenU<T, U> Property { get { return default; } }



		static  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly  /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, T> /**/ m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceGivenTU<T, T> m_field;
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, T> Property { get { return default; } }



		static /* TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /*  TypeParameterIsNotKnownToBeImmutable(T) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<T, U> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<T, U> Property { get { return default; } }


		static /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly /* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ m_field;
		/* TypeParameterIsNotKnownToBeImmutable(U) */ Types.SomeImmutableGenericInterfaceGivenTU<U, T> /**/ Property { get; }
		Types.SomeImmutableGenericInterfaceGivenTU<U, T> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, U> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T>/* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, T> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<U, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get { return default; } }



		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> Property { get { return default; } }


		static Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		static readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> /* MemberIsNotReadOnly(Field, m_field, AnalyzedImmutableGenericClassGivenT) */ m_field /**/;
		readonly Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> m_field;
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get; }
		Types.SomeImmutableGenericInterfaceRestrictingTU</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/, /* TypeParameterIsNotKnownToBeImmutable(T) */ T /**/> Property { get { return default; } }

		// These are MethodDeclarationSyntax
		void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/>() { }
		void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, U>() { }
		void SomeGenericMethodConditionallyRestrictingU<T, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }
		void SomeGenericMethodConditionallyRestrictingTU</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, /* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] U /**/>() { }

		void sealed class SomeGenericClassDoublyRestrictingT<[Immutable] [ConditionallyImmutable.OnlyIf] /* ConflictingImmutability(Immutable, ConditionallyImmutable.OnlyIf, typeparameter) */ T /**/> { }
		void sealed class SomeGenericClassRestrictingT<[Immutable] T> { }
		void sealed class SomeGenericClassConditionallyRestrictingT<[ConditionallyImmutable.OnlyIf] T> { }

		void Method()
		{
			// These are LocalFunctionStatementSyntax
			void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/>() { }
			void SomeGenericMethodConditionallyRestrictingT</* UnexpectedConditionalImmutability */ [ConditionallyImmutable.OnlyIf] T /**/, U>() { }
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
			=> (V, W, X, Y, Z) = (v, w, x, y, z);
    }

	record NonImmutableBaseRecord(object x);

	[Immutable]
	record DerivedRecordWithQuestionableBase :
		/* NonImmutableTypeHeldByImmutable(class, SpecTests.NonImmutableBaseRecord,  (or [ImmutableBaseClass])) */ NonImmutableBaseRecord(new object()) /**/ ;

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
