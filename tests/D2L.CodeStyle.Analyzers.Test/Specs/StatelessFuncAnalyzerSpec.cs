// analyzer: D2L.CodeStyle.Analyzers.Immutability.StatelessFuncAnalyzer

using System;

namespace D2L {
	public class StatelessFunc<TResult> {

		private readonly Func<TResult> m_func;

		public StatelessFunc( Func<TResult> func ) {
			m_func = func;
		}
	}

	public class StatelessFunc<T, TResult> {

		private readonly Func<T, TResult> m_func;

		public StatelessFunc( Func<T, TResult> func ) {
			m_func = func;
		}
	}
}

namespace SpecTests {

	using D2L;
	internal sealed class Usages {

		public void ParenNoClosures() {
			var func = new StatelessFunc<int>( () => 0 );
		}

		public void ParenWithClosures() {
			int zero = 0;
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt */ () => zero /**/ );
		}

		public void SimpleNoClosures() {
			var func = new StatelessFunc<string, string>( x => x + "\n" );
		}

		public void SimpleWithClosures() {
			string trailing = "\n";
			var func = new StatelessFunc<string, string>( /* StatelessFuncIsnt */ x => x + trailing /**/ );
		}

		public void NonStaticMember() {
			var func = new StatelessFunc<string>( /* StatelessFuncIsnt */ this.ToString /**/ );
		}

		public void StaticMember() {
			var func = new StatelessFunc<string, int>( Int32.Parse );
		}

		public void MultiLine() {
			var func = new StatelessFunc<int>(
				() => {
					int x = 0;
					return x + 1;
				}
			);
		}

		public void MultiLineWithThisCapture() {
			var func = new StatelessFunc<string>(
				/* StatelessFuncIsnt */ () => {
					string trailing = "\n";
					return this.ToString() + trailing;
				} /**/
			);
		}

		public void InvalidCtor() {
			var func = new StatelessFunc();
		}

		public void EvilFactory() {
			Func<int> evil() {
				int x = 0;
				return () => {
					x += 1;
					return x;
				};
			};

			var func = new StatelessFunc<int>( /* StatelessFuncIsnt */ evil() /**/ );
		}
	}
}
