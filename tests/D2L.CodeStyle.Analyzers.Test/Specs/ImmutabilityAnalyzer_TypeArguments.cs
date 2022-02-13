// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;

namespace Z;

#region Types / Members used for exercising tests. Diagnostics disabled.
#pragma warning disable

public class TestImmutableT<[Immutable] T> { }
public interface ITestImmutableU<T, [Immutable] U> { }

[Immutable] public class MyImmutable { }
public class MyMutable { }

[Immutable] public class MyImmutable<[Immutable] T> { }

public static class Holder {
	public static MyImmutable MyImmutable { get; }
	public static MyImmutable<MyImmutable> MyImmutable_MyImmutable { get; }
	public static MyImmutable<MyMutable> MyImmutable_MyMutable { get; }

	public static MyMutable MyMutable { get; }

	public static Receiver<MyMutable> Receiver_MyMutable { get; }

	public static (TestImmutableT<MyImmutable>, TestImmutableT<MyMutable>) Tuple_MyImmutable_MyMutable { get; }
}

public class Receiver {
	public Receiver Invoke<[Immutable] T>( T _ ) { }
	public static Receiver StaticInvoke<[Immutable] T>( T _ ) { }

	public static void ImmutableOut( out TestImmutableT<MyImmutable> foo );
	public static void MutableOut( out TestImmutableT<MyMutable> foo );
}

public class Receiver<[Immutable] T> {

	public static readonly Receiver<T> Instance;

	public Receiver<T> Invoke( T _ ) { }
	public static Receiver<T> StaticInvoke( T _ ) { }

	public class NestedReceiver<[Immutable] U> {
		public static readonly NestedReceiver<U> Instance;
	}
}

#pragma warning restore
#endregion

public class Tester<[Immutable] T, U> {

	// SymbolKind.Field
	TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> mutableField;

	// SymbolKind.Property
	TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> MutableProp { get; }

	void Statements<[Immutable] R, S>( U _ ) {
		// OperationKind.VariableDeclaration
		TestImmutableT<MyImmutable> test_myImmutable;
		TestImmutableT<T> test_immutableClassTP;
		TestImmutableT<R> test_immutableMethodTP;
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> test_myMutable;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> test_mutableClassTP;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> test_mutableMethodTP;
		TestImmutableT<MyImmutable<MyImmutable>> test_myImmutable_myImmutable;
		TestImmutableT<MyImmutable</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>> test_myImmutable_myMutable;
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> multiVarA, multiVarB;
		TestImmutableT<
			/* ArraysAreMutable(MyImmutable) */
			MyImmutable<
				/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/
			>[]
		/**/
		> _;
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>[][] _;
		TestImmutableT<(
			MyImmutable<T>,
			MyImmutable</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>,
			MyImmutable</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>
		)> _;
		TestImmutableT<(
			MyImmutable<T> A,
			MyImmutable</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> B,
			MyImmutable</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> C
		)> _;


		// OperationKind.DeclarationExpression & OperationKind.Discard
		var (
			_,
			/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ _ /**/
		) = Holder.Tuple_MyImmutable_MyMutable;
		var (
			_,
			/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ expression_test_myMutableA /**/
		) = Holder.Tuple_MyImmutable_MyMutable;
		(
			TestImmutableT<MyImmutable> _,
			TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _
		) = Holder.Tuple_MyImmutable_MyMutable;
		(
			TestImmutableT<MyImmutable> _,
			TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> expression_test_myMutableB
		) = Holder.Tuple_MyImmutable_MyMutable;
		Receiver.ImmutableOut( out var _ );
		Receiver.ImmutableOut( out TestImmutableT<MyImmutable> _ );
		Receiver.ImmutableOut( out var out_test_immutableA );
		Receiver.ImmutableOut( out TestImmutableT<MyImmutable> out_test_immutableB );
		Receiver.MutableOut( out /* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ var /**/ _ );
		Receiver.MutableOut( out TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> _ );
		Receiver.MutableOut( out /* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ var /**/ out_test_mutableA );
		Receiver.MutableOut( out TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> out_test_mutableB );


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


		// OperationKind.MethodReference
		var _ = Receiver.StaticInvoke</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>;


		// OperationKind.VariableDeclaration && OperationKind.MethodReference
		/* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ var /**/ _ =
			Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>.StaticInvoke;


		// OperationKind.FieldReference
		Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>.Instance;
		Receiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>
			.NestedReceiver</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>
			.Instance;


		// OperationKind.Conversion
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


		// SyntaxKind.LocalFunctionStatement
		TestImmutableT<MyImmutable> LocalMethodDeclaration( TestImmutableT<MyImmutable> a, out TestImmutableT<MyImmutable> b ) => throw null;
		TestImmutableT<T> LocalMethodDeclarationT( TestImmutableT<T> a, out TestImmutableT<T> b ) => throw null;
		TestImmutableT<R> LocalMethodDeclarationR( TestImmutableT<R> a, out TestImmutableT<R> b ) => throw null;
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> LocalMethodDeclarationMutable(
			TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> a,
			out TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> b
		) => throw null;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> LocalMethodDeclarationU(
			TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> a,
			out TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> b
		) => throw null;
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> LocalMethodDeclarationS(
			TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> a,
			out TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> b
		) => throw null;
	}

	// SymbolKind.NamedType
	public class ImplementerA : TestImmutableT<MyImmutable>, ITestImmutableT<MyImmutable> { }
	public class ImplementerB : TestImmutableT<T>, ITestImmutableT<MyImmutable> { }
	public class ImplementerC :
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/>,
		ITestImmutableU<T, /* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> { }
	public class ImplementerD :
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/>,
		ITestImmutableU<T, /* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> { }

	// SymbolKind.Method
	TestImmutableT<MyImmutable> MethodDeclaration( TestImmutableT<MyImmutable> a, out TestImmutableT<MyImmutable> b ) => throw null;
	TestImmutableT<T> MethodDeclarationT( TestImmutableT<T> a, out TestImmutableT<T> b ) => throw null;
	TestImmutableT<R> MethodDeclarationR<[Immutable] R>( TestImmutableT<R> a, out TestImmutableT<R> b ) => throw null;
	TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> MethodDeclarationMutable(
		TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> a,
		out TestImmutableT</* NonImmutableTypeHeldByImmutable(class, Z.MyMutable, ) */ MyMutable /**/> b
	) => throw null;
	TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> MethodDeclarationU(
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> a,
		out TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(U) */ U /**/> b
	) => throw null;
	TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> MethodDeclarationS<S>(
		TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> a,
		out TestImmutableT</* TypeParameterIsNotKnownToBeImmutable(S) */ S /**/> b
	) => throw null;
}
