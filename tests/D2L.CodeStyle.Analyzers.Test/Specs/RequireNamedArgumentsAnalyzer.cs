// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		private class Thing {
			public int a16;
			public int m_a16;
			public int _a16;
			public Thing nested;
		}

		public static void _arg0() { }
		public static void _arg1( int a1 ) { }
		public static void _arg2( int a1, int a2 ) { }
		public static void _arg3( int a1, int a2, int a3 ) { }
		public static void _arg4( int a1, int a2, int a3, int a4 ) { }

		// These will shrink as we shrink the max *blush*
		public static void _arg19( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19 ) { }
		public static void _arg20( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20 ) { }
		public static void _arg21( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21 ) { }

		public static void funcWithParams( int a, int b, int c, params int[] ps ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( int a1 );
		public delegate void delegate20Args( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20 );

		public sealed class SomeClass {
			public SomeClass() { }
			public SomeClass( int a1 ) { }
			public SomeClass( int a1, int a2 ) { }
			public SomeClass( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19, int a20 ) { }
		}

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( 1 );
			_arg2( 1, 2 );
			_arg3( 1, 2, 3 );
			_arg4( 1, 2, 3, 4 );
			_arg19( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 );
			#endregion

			#region diagnostic for too many unnamed args
			/* TooManyUnnamedArgs */ _arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 ) /**/;
			/* TooManyUnnamedArgs */ _arg21( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg20(
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
				a20: 20
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20: 20 );
			_arg21( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20: 20, a21: 21 );
			#endregion

			#region arguments that are literals with the correct name don't count against the budget
			int a16 = 16;
			int a20 = 20;
			int A21 = 21; // case doesn't matter
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, a16, 17, 18, 19, 20 );
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20 );
			_arg21( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20, A21 );
			#endregion

			#region member accesses can also serve as psuedo-names
			var thing = new Thing();
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, thing.a16, 17, 18, 19, 20 );
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, thing.nested.a16, 17, 18, 19, 20 );
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, thing.m_a16, 17, 18, 19, 20 );
			_arg20( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, thing.nested._a16, 17, 18, 19, 20 );
			#endregion

			#region need to have enough named args, though
			/* TooManyUnnamedArgs */ _arg21( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, a21: 21 ) /**/;
			/* TooManyUnnamedArgs */ _arg21( 1, a2: 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( 1, 2, 3 );
			funcWithParams( 1, 2, 3, 4 );
			funcWithParams( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* TooManyUnnamedArgs */ ((delegate20Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 ) /**/;
			((delegate20Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20: 20 );
			#endregion

			#region class constructors should behave the same way
			// these aren't InvocationExpressions like the above but should
			// behave just the same.
			new SomeClass();
			new SomeClass( 1 );
			new SomeClass( 1, 2 );
			/* TooManyUnnamedArgs */ new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 ) /**/;
			new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, a20: 20 );
			#endregion
		}
	}
}
