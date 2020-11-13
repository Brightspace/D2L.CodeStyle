// analyzer: D2L.CodeStyle.Analyzers.Immutability.StatelessFuncAnalyzer

using System;

namespace D2L.CodeStyle.Annotations {
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public class ReadOnlyAttribute : Attribute { }

	namespace Contract {
		[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
		public sealed class StatelessFuncAttribute : ReadOnlyAttribute { }
	}
}

namespace D2L {

	using D2L.CodeStyle.Annotations.Contract;

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
	using D2L.CodeStyle.Annotations.Contract;

	internal static class AttributeFuncReceiver {

		internal static void Accept<TResult>( [StatelessFunc] Func<TResult> f ) { }
		internal static void Accept<T1, TResult>( [StatelessFunc] Func<T1, TResult> f ) { }

	}

	internal sealed class Usages {

		public void ParenNoClosures() {
			var func = new StatelessFunc<int>( () => 0 );
			AttributeFuncReceiver.Accept<int>( () => 0 );
		}

		public void ParenWithClosures() {
			int zero = 0;
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Captured variable(s): zero) */ () => zero /**/ );
			AttributeFuncReceiver.Accept<int>( /* StatelessFuncIsnt(Captured variable(s): zero) */ () => zero /**/ );
		}

		public void SimpleNoClosures() {
			var func = new StatelessFunc<string, string>( x => x + "\n" );
			AttributeFuncReceiver.Accept<string, string>( x => x + "\n" );
		}

		public void SimpleWithClosures() {
			string trailing = "\n";
			var func = new StatelessFunc<string, string>( /* StatelessFuncIsnt(Captured variable(s): trailing) */ x => x + trailing /**/ );
			AttributeFuncReceiver.Accept<string, string>( /* StatelessFuncIsnt(Captured variable(s): trailing) */ x => x + trailing /**/ );
		}

		public void DelegateNoClosures() {
			var func = new StatelessFunc<string, string>( delegate( string x ) { return x + "\n"; } );
			AttributeFuncReceiver.Accept<string, string>( delegate ( string x ) { return x + "\n"; } );
		}

		public void DelegateWithClosures() {
			string trailing = "\n";
			var func = new StatelessFunc<string, string>(  /* StatelessFuncIsnt(Captured variable(s): trailing) */ delegate ( string x ) { return x + trailing; } /**/ );
			AttributeFuncReceiver.Accept<string, string>(  /* StatelessFuncIsnt(Captured variable(s): trailing) */ delegate ( string x ) { return x + trailing; } /**/ );
		}

		public void NonStaticMember() {
			var func = new StatelessFunc<string>( /* StatelessFuncIsnt(this.ToString is not static) */ this.ToString /**/ );
			AttributeFuncReceiver.Accept<string>( /* StatelessFuncIsnt(this.ToString is not static) */ this.ToString /**/ );
		}

		public void StaticMember() {
			var func = new StatelessFunc<string, int>( Int32.Parse );
			AttributeFuncReceiver.Accept<string, int>( Int32.Parse );
		}

		public void MultiLine() {
			var func = new StatelessFunc<int>(
				() => {
					int x = 0;
					return x + 1;
				}
			);
			AttributeFuncReceiver.Accept<int>(
				 () => {
					 int x = 0;
					 return x + 1;
				 }
			 );
		}

		public void MultiLineWithThisCapture() {
			var func = new StatelessFunc<string>(
				/* StatelessFuncIsnt(Captured variable(s): this) */ () => {
					string trailing = "\n";
					return this.ToString() + trailing;
				} /**/
			);
			AttributeFuncReceiver.Accept<string>(
				/* StatelessFuncIsnt(Captured variable(s): this) */ () => {
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

			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Invocations are not allowed: evil()) */ evil() /**/ );
			AttributeFuncReceiver.Accept<int>( /* StatelessFuncIsnt(Invocations are not allowed: evil()) */ evil() /**/ );
		}

		public void StatelessFunc() {
			var func = new StatelessFunc<int>( new StatelessFunc<int>( () => 0 ) );
			AttributeFuncReceiver.Accept( new StatelessFunc<int>( () => 0 ) );
		}

		public void StatelessFuncFromVar() {
			var f = new StatelessFunc<int>( () => 0 );

			var func = new StatelessFunc<int>( f );
			AttributeFuncReceiver.Accept( f );
		}

		public void StatelessFuncFromParam( StatelessFunc<int> f) {
			var func = new StatelessFunc<int>( f );
			AttributeFuncReceiver.Accept( f );
		}

		public void StatelessFuncAttrParam( [StatelessFunc] Func<int> f ) {
			var func = new StatelessFunc<int>( f );
			AttributeFuncReceiver.Accept( f );
		}

		public void FuncFromVar() {
			Func<int> f = () => 0;

			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Unable to determine if f is stateless.) */ f /**/ );
			AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(Unable to determine if f is stateless.) */ f /**/ );
		}

		public void FuncFromParam( Func<int> f ) {
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Unable to determine if f is stateless.) */ f /**/ );
			AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(Unable to determine if f is stateless.) */ f /**/ );
		}

		internal sealed class AnotherConstructor {

			public AnotherConstructor( int i )
				: this( /* StatelessFuncIsnt(Captured variable(s): i) */ () => ++i /**/ ) { }

			public AnotherConstructor()
				: this( DoStuff ) { }

			public AnotherConstructor( [StatelessFunc] Func<int> func ) { }

			private static int DoStuff() { return 1; }
		}

		internal class BaseClass {

			public BaseClass( [StatelessFunc] Func<int> func ) { }


			internal sealed class SubClass : BaseClass {

				public SubClass( int i )
					: base( /* StatelessFuncIsnt(Captured variable(s): i) */ () => ++i /**/ ) { }

			}
		}
	}
}
