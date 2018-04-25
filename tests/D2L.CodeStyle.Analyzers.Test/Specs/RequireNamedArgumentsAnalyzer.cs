// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		public void _arg0() { }
		public void _arg1( int a1 ) { }
		public void _arg2( int a1, int a2 ) { }
		public void _arg3( int a1, int a2, int a3 ) { }
		public void _arg4( int a1, int a2, int a3, int a4 ) { }
		public void _arg5( int a1, int a2, int a3, int a4, int a5 ) { }
		public void _arg6( int a1, int a2, int a3, int a4, int a5, int a6 ) { }
		public void _arg7( int a1, int a2, int a3, int a4, int a5, int a6, int a7 ) { }
		public void _arg8( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8 ) { }
		public void _arg9( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9 ) { }
		public void _arg10( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10 ) { }
		public void _arg11( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11 ) { }

		public void funcWithParams( int a, int b, int c, params int[] ps ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( int a1 );
		public delegate void delegate10Args( int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10 );

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( 1 );
			_arg2( 1, 2 );
			_arg3( 1, 2, 3 );
			_arg4( 1, 2, 3, 4 );
			_arg5( 1, 2, 3, 4, 5 );
			_arg6( 1, 2, 3, 4, 5, 6 );
			_arg7( 1, 2, 3, 4, 5, 6, 7 );
			_arg8( 1, 2, 3, 4, 5, 6, 7, 8 );
			_arg9( 1, 2, 3, 4, 5, 6, 7, 8, 9 );
			#endregion

			#region diagnostic for too many unnamed args
			/* UseNamedArgsForInvocationWithLotsOfArgs */ _arg10( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ) /**/;
			/* UseNamedArgsForInvocationWithLotsOfArgs */ _arg11( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg10(
				a1: 1,
				a2: 2,
				a3: 3,
				a4: 4,
				a5: 5,
				a6: 6,
				a7: 7,
				a8: 8,
				a9: 9,
				a10: 10
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg10( 1, 2, 3, 4, 5, 6, 7, 8, 9, a10: 10 );
			_arg11( 1, 2, 3, 4, 5, 6, 7, 8, 9, a10: 10, a11: 11 );
			#endregion

			/* UseNamedArgsForInvocationWithLotsOfArgs */ _arg11( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, a11: 11 ) /**/;

			#region params don't count against the unnamed args budget
			funcWithParams( 1, 2, 3 );
			funcWithParams( 1, 2, 3, 4 );
			funcWithParams( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( 1 );
			/* UseNamedArgsForInvocationWithLotsOfArgs */ ((delegate10Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ) /**/;
			((delegate10Args)null)( 1, 2, 3, 4, 5, 6, 7, 8, 9, a10: 10 );
			#endregion
		}
	}
}