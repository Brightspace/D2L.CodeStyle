// analyzer: D2L.CodeStyle.Analyzers.Immutability.StatelessFuncAnalyzer

using System;

namespace D2L {

	public class StatelessFuncAttribute : Attribute { }

	public class StatelessFunc<TResult> {
		public StatelessFunc( [StatelessFunc] Func<TResult> func ) { }
		public static implicit operator Func<TResult>( StatelessFunc<TResult> @this ) { }
	}

	public class StatelessFunc<T, TResult> {
		public StatelessFunc( [StatelessFunc] Func<T, TResult> func ) { }
		public static implicit operator Func<T, TResult>( StatelessFunc<T, TResult> @this ) { }
	}

}

namespace SpecTests {

	using D2L;

	internal static class AttributeFuncReceiver {

		internal static void Accept<TResult>( [StatelessFunc] Func<TResult> f ) { }
		internal static void Accept<T1, TResult>( [StatelessFunc] Func<T1, TResult> f ) { }

	}

	internal sealed class Usages {
		public void StatelessFunc() {

			var f = new StatelessFunc<int>( () => 0 );

			var func = new StatelessFunc<int>( f );
			AttributeFuncReceiver.Accept( f );

		}
	}
}
