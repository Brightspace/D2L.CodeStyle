// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		public void _arg0() { }
		public void _arg1( int a1 ) { }
		public void _arg2( int a1, int a2 ) { }
		public void _arg3( int a1, int a2, int a3 ) { }
		public void _arg4( int a1, int a2, int a3, int a4 ) { }

		// These will shrink as we shrink the max *blush*
		public void _arg30( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29, int a30 );
		public void _arg31( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29, int a30, int a31 );
		public void _arg32( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29, int a30, int a31, int a32 );

		public void funcWithParams( int a, int b, int c, params int[] ps ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( int a1 );
		public delegate void delegate31Args( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29, int a30, int a31 );

		public sealed class SomeClass {
			public SomeClass() { }
			public SomeClass( int a1 ) { }
			public SomeClass( int a1, int a2 ) { }
			public SomeClass( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29, int a30, int a31 );
		}

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( 1 );
			_arg2( 1, 2 );
			_arg3( 1, 2, 3 );
			_arg4( 1, 2, 3, 4 );
			_arg30( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 );
			#endregion

			#region diagnostic for too many unnamed args
			/* UseNamedArgsWhenTooManyArgs */ _arg31( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 ) /**/;
			/* UseNamedArgsWhenTooManyArgs */ _arg32( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg31(
				a1: 1,
				a2: 2,
				a3: 3,
				a4: 4,
				a5: 5,
				a6: 6,
				a7: 7,
				a8: 8,
				a9: 9,
				a10: 10,
				a11: 11,
				a12: 12,
				a13: 13,
				a14: 14,
				a15: 15,
				a16: 16,
				a17: 17,
				a18: 18,
				a19: 19,
				a20: 20,
				a21: 21,
				a22: 22,
				a23: 23,
				a24: 24,
				a25: 25,
				a26: 26,
				a27: 27,
				a28: 28,
				a29: 29,
				a30: 30,
				a31: 31
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg31( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, a31: 31 );
			_arg32( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, a31: 31, a32: 32 );
			#endregion

			#region need to have enough named args, though
			/* UseNamedArgsWhenTooManyArgs */ _arg32( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, a32: 32 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( 1, 2, 3 );
			funcWithParams( 1, 2, 3, 4 );
			funcWithParams( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* UseNamedArgsWhenTooManyArgs */ ((delegate31Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 ) /**/;
			((delegate31Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, a31: 31 );
			#endregion

			#region class constructors should behave the same way
			// these aren't InvocationExpressions like the above but should
			// behave just the same.
			new SomeClass();
			new SomeClass( 1 );
			new SomeClass( 1, 2 );
			/* UseNamedArgsWhenTooManyArgs */ new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 ) /**/;
			new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, a31:31 );

			#endregion
		}
	}
}