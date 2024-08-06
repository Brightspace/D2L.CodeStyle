// analyzer: D2L.CodeStyle.Analyzers.Immutability.ReadOnlyParameterAnalyzer, D2L.CodeStyle.Analyzers

using System;

namespace SpecTests {

	using D2L.CodeStyle.Annotations;

	internal sealed class ReadOnlyAttributeUsages {

		void Unused( [ReadOnly] int foo ) {}

		void OnlyRead( [ReadOnly] int foo ) {
			int bar = foo;
		}

		int OnlyReadExpressionBody( [ReadOnly] int foo ) => foo;

		void PassedByValue( [ReadOnly] int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( [ReadOnly] int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( [ReadOnly] int foo ) {
			return foo;
		}

		void WrittenToInBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			() => { foo = 1; };
		}

		void PassedToRef( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			RefParameter( ref foo );
		}

		void PassedToRefExpressionBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ )
			=> RefParameter( ref foo );

		void RefParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnly] ref int foo /**/ ) { }
		void InParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnly] in int foo /**/ ) { }

		internal class C {
			C( [ReadOnly] int foo ) { }
		}

		internal class CProblem {
			CProblem( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
				foo = 1;
			}
		}

		internal interface I {
			void Foo( [ReadOnly] int foo );
		}

		void LocalFunctionTests() {

			void WrittenToInBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int value /**/ ) {
				value = 1;
			}
		}

		#endregion
	}

	internal sealed class SubclassAttributeUsages {

		[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
		public sealed class ReadOnlySubclassAttribute : ReadOnlyAttribute { }

		void Unused( [ReadOnlySubclass] int foo ) { }

		void OnlyRead( [ReadOnlySubclass] int foo ) {
			int bar = foo;
		}

		void PassedByValue( [ReadOnlySubclass] int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( [ReadOnlySubclass] int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( [ReadOnlySubclass] int foo ) {
			return foo;
		}

		void WrittenToInBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			() => { foo = 1; };
		}

		void PassedToRef( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			RefParameter( ref foo );
		}

		void RefParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnlySubclass] ref int foo /**/ ) { }
		void InParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnlySubclass] in int foo /**/ ) { }

		internal class C {
			C( [ReadOnlySubclass] int foo ) { }
		}

		internal class CProblem {
			CProblem( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
				foo = 1;
			}
		}

		internal interface I {
			void Foo( [ReadOnlySubclass] int foo );
		}

	}

	internal partial class PartialMethodUsage {
		internal partial void Foo( [ReadOnly] int foo );
		internal partial void Bar( [ReadOnly] int bar );
		internal partial void Baz( int baz );
		internal partial void Quux( [ReadOnly] int quux );
		internal partial void Foobar( int foobar );
	}
	internal partial class PartialMethodUsage {
		internal partial void Foo( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ int foo /**/ ) {
			foo = 1;
		}
		internal partial void Bar( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int bar /**/ ) {
			bar = 1;
		}
		internal partial void Baz( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int baz /**/ ) {
			baz = 1;
		}
		internal partial void Quux( int quux ) {
			int x = quux;
		}
		internal partial void Baz( [ReadOnly] int foobar ) {
			int x = foobar;
		}
	}

	internal sealed class NonReadOnlyThings {

		void Unused( int foo ) { }

		void OnlyRead( int foo ) {
			int bar = foo;
		}

		void PassedByValue( int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( int foo ) {
			return foo;
		}

		void WrittenToInBody( int foo ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( int foo ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( int foo ) {
			() => { foo = 1; };
		}

		void PassedToRef( int foo ) {
			RefParameter( ref foo );
		}

		void RefParameter( ref int foo ) { }
		void InParameter( in int foo ) { }

	}
}
