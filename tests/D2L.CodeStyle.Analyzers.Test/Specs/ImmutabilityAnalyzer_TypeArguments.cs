// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;

namespace Z;

#pragma warning disable D2L0066

public class TestImmutableT<[Immutable] T> { }
public class TestImmutableU<T, [Immutable] U> { }

[Immutable] public class MyImmutable { }
public class MyMutable { }

[Immutable] public class MyImmutable<[Immutable] T> { }

public static class Holder {
	public static MyImmutable MyImmutable { get; }
	public static MyImmutable<MyImmutable> MyImmutable_MyImmutable { get; }
	public static MyImmutable<MyMutable> MyImmutable_MyMutable { get; }

	public static MyMutable MyMutable { get; }

	public static Receiver<MyMutable> Receiver_MyMutable { get; }
}

public class Receiver {
	public Receiver Invoke<[Immutable] T>( T _ ) { }
	public static Receiver StaticInvoke<[Immutable] T>( T _ ) { }
}

public class Receiver<[Immutable] T> {

	public static readonly Receiver<T> Instance;

	public Receiver<T> Invoke( T _ ) { }
	public static Receiver<T> StaticInvoke( T _ ) { }
}

#pragma warning restore D2L0066

public class Tester<[Immutable] T, U> {

	// SymbolKind.Field
	TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> mutableField;

	// SymbolKind.Property
	TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> MutableProp { get; }

	void Statements<[Immutable] R, S>( U _ ) {
		// Variable Declaration
		TestImmutableT<MyImmutable> test_myImmutable;
		TestImmutableT<T> test_immutableClassTP;
		TestImmutableT<R> test_immutableMethodTP;
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> test_myMutable;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> test_mutableClassTP;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> test_mutableMethodTP;

		// Variable Declaration, nested immutable
		TestImmutableT<MyImmutable<MyImmutable>> test_myImmutable_myImmutable;
		TestImmutableT<MyImmutable</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>> test_myImmutable_myMutable;

		// OperationKind.ObjectCreation
		test_myImmutable = new();
		test_immutableClassTP = new();
		test_immutableMethodTP = new();
		test_myMutable = /* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ new /**/ ();
		test_mutableClassTP = /* TypeParameterIsNotKnownToBeImmutable(U) */ new /**/ ();
		test_mutableMethodTP = /* TypeParameterIsNotKnownToBeImmutable(S) */ new /**/ ();
		test_myImmutable_myImmutable = new();
		test_myImmutable_myMutable = /* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ new /**/ ();
		new TestImmutableT<MyImmutable>();
		new TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>();
		new TestImmutableT<MyImmutable<MyImmutable>>();
		new TestImmutableT<MyImmutable</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>>();

		// OperationKind.Invocation
		Receiver
			.StaticInvoke</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>( Holder.MyMutable )
			.Invoke</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>( Holder.MyMutable );

		Receiver
			./* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ StaticInvoke /**/ ( Holder.MyMutable )
			./* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ Invoke /**/ ( Holder.MyMutable );

		Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>
			.StaticInvoke( Holder.MyMutable );

		Holder.Receiver_MyMutable.Invoke( Holder.MyMutable );

		TestImmutableT<
			/* ArraysAreMutable(MyImmutable) */
			MyImmutable<
				/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/
			>[]
			/**/
		> _;


		// OperationKind.MethodReference
		var _ = Receiver.StaticInvoke</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>;


		// OperationKind.VariableDeclaration && OperationKind.MethodReference
		/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ var /**/ _ =
			Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>.StaticInvoke;


		// OperationKind.FieldReference
		Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>.Instance;

		(TestImmutableT<MyImmutable>)null;
		(TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>)null;

		null as TestImmutableT<MyImmutable>;
		null as TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>;

		// OperationKind.TypeOf
		Type _ = typeof( TestImmutableT<MyImmutable> );
		Type _ = typeof( TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> );


		// OperationKind.IsType
		if( new object() is TestImmutableT<MyImmutable> ) { }
		if( new object() is TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> ) { }


		// OperationKind.TypePattern
		if( new object() is not TestImmutableT<MyImmutable> ) { }
		if( new object() is not TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> ) { }


		// OperationKind.DeclarationPattern
		if( new object() is TestImmutableT<MyImmutable> _ ) { }
		if( new object() is TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _ ) { }
		if( new object() is not TestImmutableT<MyImmutable> _ ) { }
		if( new object() is not TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _ ) { }


		switch( new object() ) {

			// OperationKind.TypePattern
			case TestImmutableT<MyImmutable>: break;
			case TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>: break;


			// OperationKind.DeclarationPattern
			case TestImmutableT<MyImmutable> _: break;
			case TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _: break;

			default: break;
		}

		var _ = new object() switch {

			// OperationKind.TypePattern
			TestImmutableT<MyImmutable> => null,
			TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> => null,


			// OperationKind.DeclarationPattern
			TestImmutableT<MyImmutable> _ => null,
			TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _ => null,

			_ => null
		};
	}

	// SymbolKind.NamedType
	public class ImplementerA : TestImmutableT<MyImmutable> { }
	public class ImplementerB : TestImmutableT<T> { }
	public class ImplementerC : TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> { }
	public class ImplementerD : TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> { }
}
