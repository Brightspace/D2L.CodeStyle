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
		public static void _arg8( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8 ) { }
		public static void _arg9( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9 ) { }
		public static void _arg10( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10 ) { }

		public static void funcWithParams( int a, int b, int c, params int[] ps ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( int a1 );
		public delegate void delegate9Args( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9 );

		public sealed class SomeClass {
			public SomeClass() { }
			public SomeClass( int a1 ) { }
			public SomeClass( int a1, int a2 ) { }
			public SomeClass( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9 ) { }
		}

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( 1 );
			_arg2( 1, 2 );
			_arg3( 1, 2, 3 );
			_arg4( 1, 2, 3, 4 );
			_arg8( 1, 2, 3, 4, 5, 6, 7, 8 );
			#endregion

			#region diagnostic for too many unnamed args
			/* TooManyUnnamedArgs */ _arg9( 1, 2, 3, 4, 5, 6, 7, 8, 9 ) /**/;
			/* TooManyUnnamedArgs */ _arg10( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg9(
				a1: 1,
				a2: 2,
				a3: 3,
				a4: 4,
				a5: 5,
				a6: 6,
				a7: 7,
				a8: 8,
				a9: 9
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg9( 1, 2, 3, 4, 5, 6, 7, 8, a9: 9 );
			_arg10( 1, 2, 3, 4, 5, 6, 7, 8, a9: 9, a10: 10 );
			#endregion

			#region arguments that are literals with the correct name don't count against the budget
			int a6 = 16;
			int a9 = 20;
			int A10 = 10; // upper case doesn't matter
			_arg9( 1, 2, 3, 4, 5, a6, 7, 8, 9 );
			_arg9( 1, 2, 3, 4, 5, 6, 7, 8, a9 );
			_arg10( 1, 2, 3, 4, 5, 6, 7, 8, a9, A10 );
			#endregion

			#region member accesses can also serve as psuedo-names
			var thing = new Thing();
			_arg9( 1, 2, 3, 4, 5, thing.a6, 7, 8, 9 );
			_arg9( 1, 2, 3, 4, 5, thing.nested.a6, 7, 8, 9 );
			_arg9( 1, 2, 3, 4, 5, thing.m_a6, 7, 8, 9 );
			_arg9( 1, 2, 3, 4, 5, thing.nested._a6, 7, 8, 9 );
			#endregion

			#region need to have enough named args, though
			/* TooManyUnnamedArgs */ _arg10( 1, 2, 3, 4, 5, 6, 7, 8, 9, a10: 10 ) /**/;
			/* TooManyUnnamedArgs */ _arg10( 1, a2: 2, 3, 4, 5, 6, 7, 8, 9, 10 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( 1, 2, 3 );
			funcWithParams( 1, 2, 3, 4 );
			funcWithParams( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* TooManyUnnamedArgs */ ((delegate9Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9 ) /**/;
			((delegate9Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, a9: 9 );
			#endregion

			#region class constructors should behave the same way
			// these aren't InvocationExpressions like the above but should
			// behave just the same.
			new SomeClass();
			new SomeClass( 1 );
			new SomeClass( 1, 2 );
			/* TooManyUnnamedArgs */ new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, 9 ) /**/;
			new SomeClass( 1, 2, 3, 4, 5, 6, 7, 8, a9: 9 );
			#endregion
		}
	}
}
