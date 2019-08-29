// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		private class Thing {
			public int a4;
			public int m_a4;
			public int _a4;
			public Thing nested;
		}

        private static int _a1, _a2, _a3, _a4, _a5, _a6, _a7, _a8, _a9, p; // "placeholder" variables

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
            _arg1( _a1 );
			_arg2( _a1, _a2 );
			_arg3( _a1, _a2, _a3 );
			_arg4( _a1, _a2, _a3, _a4 );

            // Named literals
            _arg5(a1: 1, a2: 2, a3: 3, a4: 4, a5: 5);
            _arg6(a1: 1, a2: 2, a3: 3, a4: 4, a5: 5, a6: 6);

            // Named literals + variables
            _arg4(_a1, _a2, a3: 3, a4: 4);
            _arg5(_a1, _a2, a3: 3, a4: 4, _a5);
            #endregion

            #region diagnostic for too many unnamed args
            /* TooManyUnnamedArgs */ _arg5(1, 2, 3, 4, 5) /**/;
            /* TooManyUnnamedArgs */ _arg6(1, 2, 3, 4, 5, 6) /**/;
            _arg3( /* LiteralArgShouldBeNamed(a1) */ 1 /**/, /* LiteralArgShouldBeNamed(a2) */ 2 /**/, /* LiteralArgShouldBeNamed(a3) */ 3 /**/ );
            _arg3( a1: 1, /* LiteralArgShouldBeNamed(a2) */ 2 /**/, /* LiteralArgShouldBeNamed(a3) */ 3 /**/ );
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
			_arg5( a1: 1, p, p, p, p );
			_arg6( a1: 1, a2: 2, p, p, p, p );
			#endregion

			#region arguments that are literals with the correct name don't count against the budget
			int a1 = 11;
			int a3 = 13;
			int A1 = 101; // upper case doesn't matter
			_arg5( a1, p, p, p, p );
			_arg5( p, p, a3, p, p );
			_arg6( A1, p, a3, p, p, p );
			#endregion

			#region member accesses can also serve as psuedo-names
			var thing = new Thing();
			_arg5( p, p, p, thing.a4, p );
			_arg5( p, p, p, thing.nested.a4, p );
			_arg5( p, p, p, thing.m_a4, p );
			_arg5( p, p, p, thing.nested._a4, p );
			#endregion

			#region need to have enough named args, though
			/* TooManyUnnamedArgs */ _arg6( a1: 1, 2, 3, 4, 5, 6 ) /**/;
			/* TooManyUnnamedArgs */ _arg6( 1, a2: 2, 3, 4, 5, 6 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( p, p, p );
			funcWithParams( p, p, p, p );
			funcWithParams( p, p, p, p, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* TooManyUnnamedArgs */ ((delegate5Args)null)( p, p, p, p, p ) /**/;
			((delegate5Args)null)( a1: 1, p, p, p, p );
			#endregion

			#region class constructors should behave the same way
			// these aren't InvocationExpressions like the above but should
			// behave just the same.
			new SomeClass();
			new SomeClass( 1 );
			new SomeClass( p, p );
			/* TooManyUnnamedArgs */ new SomeClass( p, p, p, p, p ) /**/;
			new SomeClass( a1: 1, p, p, p, p );
			#endregion
		}
	}
}
