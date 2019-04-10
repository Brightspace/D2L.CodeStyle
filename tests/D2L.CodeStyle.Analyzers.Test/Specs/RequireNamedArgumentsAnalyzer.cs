// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		private class Thing {
			public int a4;
			public int m_a4;
			public int _a4;
			public Thing nested;
		}

		public static void _arg0() { }
		public static void _arg1( int a1 ) { }
		public static void _arg2( int a1, int a2 ) { }
		public static void _arg3( int a1, int a2, int a3 ) { }
		public static void _arg4( int a1, int a2, int a3, int a4 ) { }

		// These will shrink as we shrink the max *blush*
		public static void _arg5( int a1, int a2, int a3, int a4, int a5 ) { }
		public static void _arg6( int a1, int a2, int a3, int a4, int a5, int a6 ) { }

		public static void funcWithParams( int a, int b, int c, params int[] ps ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( int a1 );
		public delegate void delegate5Args( int a1, int a2, int a3, int a4, int a5 );

		public sealed class SomeClass {
			public SomeClass() { }
			public SomeClass( int a1 ) { }
			public SomeClass( int a1, int a2 ) { }
			public SomeClass( int a1, int a2, int a3, int a4, int a5 ) { }
		}

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( 1 );
			_arg2( 1, 2 );
			_arg3( 1, 2, 3 );
			_arg4( 1, 2, 3, 4 );
			#endregion

			#region diagnostic for too many unnamed args
			/* TooManyUnnamedArgs */ _arg5( 1, 2, 3, 4, 5 ) /**/;
			/* TooManyUnnamedArgs */ _arg6( 1, 2, 3, 4, 5, 6 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg6(
				a1: 1,
				a2: 2,
				a3: 3,
				a4: 4,
				a5: 5,
				a6: 6
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg5( a1: 1, 2, 3, 4, 5 );
			_arg6( a1: 1, a2: 2, 3, 4, 5, 6 );
			#endregion

			#region arguments that are literals with the correct name don't count against the budget
			int a1 = 11;
			int a3 = 13;
			int A1 = 101; // upper case doesn't matter
			_arg5( a1, 2, 3, 4, 5 );
			_arg5( 1, 2, a3, 4, 5 );
			_arg6( A1, 2, a3, 4, 5, 6 );
			#endregion

			#region member accesses can also serve as psuedo-names
			var thing = new Thing();
			_arg5( 1, 2, 3, thing.a4, 5 );
			_arg5( 1, 2, 3, thing.nested.a4, 5 );
			_arg5( 1, 2, 3, thing.m_a4, 5 );
			_arg5( 1, 2, 3, thing.nested._a4, 5 );
			#endregion

			#region need to have enough named args, though
			/* TooManyUnnamedArgs */ _arg6( a1: 1, 2, 3, 4, 5, 6 ) /**/;
			/* TooManyUnnamedArgs */ _arg6( 1, a2: 2, 3, 4, 5, 6 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( 1, 2, 3 );
			funcWithParams( 1, 2, 3, 4 );
			funcWithParams( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* TooManyUnnamedArgs */ ((delegate5Args)null)( 1, 2, 3, 4, 5 ) /**/;
			((delegate5Args)null)( a1: 1, 2, 3, 4, 5 );
			#endregion

			#region class constructors should behave the same way
			// these aren't InvocationExpressions like the above but should
			// behave just the same.
			new SomeClass();
			new SomeClass( 1 );
			new SomeClass( 1, 2 );
			/* TooManyUnnamedArgs */ new SomeClass( 1, 2, 3, 4, 5 ) /**/;
			new SomeClass( a1: 1, 2, 3, 4, 5 );
			#endregion
		}
	}
}
