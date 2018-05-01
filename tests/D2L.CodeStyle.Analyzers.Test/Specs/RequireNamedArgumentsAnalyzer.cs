// analyzer: D2L.CodeStyle.Analyzers.Language.RequireNamedArgumentsAnalyzer

namespace D2L {
	public static class Foo {
		public sealed class T { }

		#region dummy values
		public static readonly T v1 = new T();
		public static readonly T v2 = v1;
		public static readonly T v3 = v1;
		public static readonly T v4 = v1;
		public static readonly T v5 = v1;
		public static readonly T v6 = v1;
		public static readonly T v7 = v1;
		public static readonly T v8 = v1;
		public static readonly T v9 = v1;
		public static readonly T v10 = v1;
		public static readonly T v11 = v1;
		public static readonly T v12 = v1;
		public static readonly T v13 = v1;
		public static readonly T v14 = v1;
		public static readonly T v15 = v1;
		public static readonly T v16 = v1;
		public static readonly T v17 = v1;
		public static readonly T v18 = v1;
		public static readonly T v19 = v1;
		public static readonly T v20 = v1;
		public static readonly T v21 = v1;
		public static readonly T v22 = v1;
		public static readonly T v23 = v1;
		public static readonly T v24 = v1;
		public static readonly T v25 = v1;
		public static readonly T v26 = v1;
		public static readonly T v27 = v1;
		public static readonly T v28 = v1;
		public static readonly T v29 = v1;
		public static readonly T v30 = v1;
		public static readonly T v31 = v1;
		public static readonly T v32 = v1;
		public static readonly T v33 = v1;
		#endregion

		public void _arg0() { }
		public void _arg1( T a1 ) { }
		public void _arg2( T a1, T a2 ) { }
		public void _arg3( T a1, T a2, T a3 ) { }
		public void _arg4( T a1, T a2, T a3, T a4 ) { }

		// These will shrink as we shrink the max *blush*
		public void _arg30( T a1, T a2, T a3, T a4, T a5, T a6, T a7, T a8, T a9, T a10, T a11, T a12, T a13, T a14, T a15, T a16, T a17, T a18, T a19, T a20, T a21, T a22, T a23, T a24, T a25, T a26, T a27, T a28, T a29, T a30 );
		public void _arg31( T a1, T a2, T a3, T a4, T a5, T a6, T a7, T a8, T a9, T a10, T a11, T a12, T a13, T a14, T a15, T a16, T a17, T a18, T a19, T a20, T a21, T a22, T a23, T a24, T a25, T a26, T a27, T a28, T a29, T a30, T a31 );
		public void _arg32( T a1, T a2, T a3, T a4, T a5, T a6, T a7, T a8, T a9, T a10, T a11, T a12, T a13, T a14, T a15, T a16, T a17, T a18, T a19, T a20, T a21, T a22, T a23, T a24, T a25, T a26, T a27, T a28, T a29, T a30, T a31, T a32 );

		public void funcWithParams( T a, T b, T c, params T[] ps ) { }

		public void funcWithObjectParam( T a, T b, T c, object d ) { }

		public delegate void delegate0Args();
		public delegate void delegate1Args( T a1 );
		public delegate void delegate31Args( T a1, T a2, T a3, T a4, T a5, T a6, T a7, T a8, T a9, T a10, T a11, T a12, T a13, T a14, T a15, T a16, T a17, T a18, T a19, T a20, T a21, T a22, T a23, T a24, T a25, T a26, T a27, T a28, T a29, T a30, T a31 );

		public static void Test() {
			#region "low" number of args doesn't require naming
			_arg0();
			_arg1( v1 );
			_arg2( v1, v2 );
			_arg3( v1, v2, v3 );
			_arg4( v1, v2, v3, v4 );
			_arg30( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30 );
			#endregion

			#region diagnostic for too many unnamed args
			/* TooManyUnnamedArgs */ _arg31( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, v31 ) /**/;
			/* TooManyUnnamedArgs */ _arg32( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, v31, v32 ) /**/;
			#endregion

			#region all named args is usually preferred if there are lots of args
			_arg31(
				a1: v1,
				a2: v2,
				a3: v3,
				a4: v4,
				a5: v5,
				a6: v6,
				a7: v7,
				a8: v8,
				a9: v9,
				a10: v10,
				a11: v11,
				a12: v12,
				a13: v13,
				a14: v14,
				a15: v15,
				a16: v16,
				a17: v17,
				a18: v18,
				a19: v19,
				a20: v20,
				a21: v21,
				a22: v22,
				a23: v23,
				a24: v24,
				a25: v25,
				a26: v26,
				a27: v27,
				a28: v28,
				a29: v29,
				a30: v30,
				a31: v31
			);
			#endregion

			#region named args don't count against the unnamed args budget
			_arg31( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, a31: v31 );
			_arg32( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, a31: v31, a32: v32 );
			#endregion

			#region need to have enough named args, though
			/* TooManyUnnamedArgs */ _arg32( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, v31, a32: v32 ) /**/;
			#endregion

			#region params don't count against the unnamed args budget
			funcWithParams( v1, v2, v3 );
			funcWithParams( v1, v2, v3, v4 );
			funcWithParams( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26 );
			#endregion

			#region delegates
			((delegate0Args)null)();
			((delegate1Args)null)( v1 );
			/* TooManyUnnamedArgs */ ((delegate31Args)null)( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, v31 ) /**/;
			((delegate31Args)null)( v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20, v21, v22, v23, v24, v25, v26, v27, v28, v29, v30, a31: v31 );
			#endregion

			#region Literals need names
			/* LiteralArgShouldBeNamed(d) */ funcWithObjectParam( v1, v2, v3, null ) /**/;
			/* LiteralArgShouldBeNamed(d) */ funcWithObjectParam( v1, v2, v3, "" ) /**/;
			/* LiteralArgShouldBeNamed(d) */ funcWithObjectParam( v1, v2, v3, 0 ) /**/;
			#endregion
		}
	}
}
