// analyzer: D2L.CodeStyle.Analyzers.Immutability.StatelessFuncAnalyzer

using System;

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
		internal static void AcceptDefault<TResult>( [StatelessFunc] Func<TResult> f = null ) { }

	}

	internal sealed class Usages {

		public void ParenStatic() {
			var func = new StatelessFunc<int>( static () => 0 );
			AttributeFuncReceiver.Accept<int>( static () => 0 );
		}

		public void ParenNoClosures() {
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Lambda is not static) */ () => 0 /**/ );
			AttributeFuncReceiver.Accept<int>( /* StatelessFuncIsnt(Lambda is not static) */ () => 0 /**/ );
		}

		public void ParenWithClosures() {
			int zero = 0;
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Lambda is not static) */ () => zero /**/ );
			AttributeFuncReceiver.Accept<int>( /* StatelessFuncIsnt(Lambda is not static) */ () => zero /**/ );
		}

		public void SimpleStatic() {
			var func = new StatelessFunc<string, string>( static x => x + "\n" );
			AttributeFuncReceiver.Accept<string, string>( static x => x + "\n" );
		}

		public void SimpleNoClosures() {
			var func = new StatelessFunc<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ x => x + "\n" /**/ );
			AttributeFuncReceiver.Accept<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ x => x + "\n" /**/ );
		}

		public void SimpleWithClosures() {
			string trailing = "\n";
			var func = new StatelessFunc<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ x => x + trailing /**/ );
			AttributeFuncReceiver.Accept<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ x => x + trailing /**/ );
		}

		public void LambdaStatic() {
			var func = new StatelessFunc<string, string>( static delegate ( string x ) { return x + "\n"; } );
			AttributeFuncReceiver.Accept<string, string>( static delegate ( string x ) { return x + "\n"; } );
		}

		public void LambdaNoClosures() {
			var func = new StatelessFunc<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ delegate ( string x ) { return x + "\n"; } /**/ );
			AttributeFuncReceiver.Accept<string, string>( /* StatelessFuncIsnt(Lambda is not static) */ delegate ( string x ) { return x + "\n"; } /**/ );
		}

		public void LambdaWithClosures() {
			string trailing = "\n";
			var func = new StatelessFunc<string, string>(  /* StatelessFuncIsnt(Lambda is not static) */ delegate ( string x ) { return x + trailing; } /**/ );
			AttributeFuncReceiver.Accept<string, string>(  /* StatelessFuncIsnt(Lambda is not static) */ delegate ( string x ) { return x + trailing; } /**/ );
		}

		public void NonStaticMember() {
			var func = new StatelessFunc<string>( /* StatelessFuncIsnt(this.ToString is not static) */ this.ToString /**/ );
			AttributeFuncReceiver.Accept<string>( /* StatelessFuncIsnt(this.ToString is not static) */ this.ToString /**/ );
		}

		public void StaticMember() {
			var func = new StatelessFunc<string, int>( Int32.Parse );
			AttributeFuncReceiver.Accept<string, int>( Int32.Parse );
		}

		public void DefaultArgument() {
			AttributeFuncReceiver.AcceptDefault<string>();
		}

		public void MultiLineStatic() {
			var func = new StatelessFunc<int>(
				static () => {
					int x = 0;
					return x + 1;
				}
			);
			AttributeFuncReceiver.Accept<int>(
				 static () => {
					int x = 0;
					return x + 1;
				}
			 );
		}

		public void MultiLine() {
			var func = new StatelessFunc<int>(
				/* StatelessFuncIsnt(Lambda is not static) */ () => {
					int x = 0;
					return x + 1;
				} /**/
			);
			AttributeFuncReceiver.Accept<int>(
				 /* StatelessFuncIsnt(Lambda is not static) */ () => {
					 int x = 0;
					 return x + 1;
				 } /**/
			 );
		}

		public void MultiLineWithThisCapture() {
			var func = new StatelessFunc<string>(
				/* StatelessFuncIsnt(Lambda is not static) */ () => {
					string trailing = "\n";
					return this.ToString() + trailing;
				} /**/
			);
			AttributeFuncReceiver.Accept<string>(
				/* StatelessFuncIsnt(Lambda is not static) */ () => {
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
			var func = new StatelessFunc<int>( new StatelessFunc<int>( static () => 0 ) );
			AttributeFuncReceiver.Accept( new StatelessFunc<int>( static () => 0 ) );
		}

		public void StatelessFuncFromVar() {
			var f = new StatelessFunc<int>( static () => 0 );

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
			var func = new StatelessFunc<int>( /* StatelessFuncIsnt(Parameter f is not marked [StatelessFunc].) */ f /**/ );
			AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(Parameter f is not marked [StatelessFunc].) */ f /**/ );
		}

		public void StatelessFuncFromInvocation() {
			StatelessFunc<int> getStatelessFunc() {
				return null;
			}

			var func = new StatelessFunc<int>( getStatelessFunc() );
			AttributeFuncReceiver.Accept( getStatelessFunc() );
		}

		internal sealed class AnotherConstructor {

			public AnotherConstructor( int i )
				: this( /* StatelessFuncIsnt(Lambda is not static) */ () => ++i /**/ ) { }

			public AnotherConstructor()
				: this( DoStuff ) { }

			public AnotherConstructor( [StatelessFunc] Func<int> func ) { }

			private static int DoStuff() { return 1; }
		}

		internal class BaseClass {

			public BaseClass( [StatelessFunc] Func<int> func ) { }


			internal sealed class SubClass : BaseClass {

				public SubClass( int i )
					: base( /* StatelessFuncIsnt(Lambda is not static) */ () => ++i /**/ ) { }

			}
		}

		internal class ClassWithMembers {

			private readonly Func<int> m_nonStaticField = static () => 1;
			private static readonly Func<int> m_staticField = static () => 2;

			private Func<int> NonStaticProperty { get; } = static () => 3;
			private static Func<int> StaticProperty { get; } = static () => 4;

			private int NonStaticMethod() => 5;
			private static int StaticMethod() => 6;

			public void ReferencesMembers() {

				var f1 = new StatelessFunc<int>( /* StatelessFuncIsnt(m_nonStaticField is not static) */ m_nonStaticField /**/ );
				AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(m_nonStaticField is not static) */ m_nonStaticField /**/ );

				var f2 = new StatelessFunc<int>( m_staticField );
				AttributeFuncReceiver.Accept( m_staticField );

				var f3 = new StatelessFunc<int>( /* StatelessFuncIsnt(NonStaticProperty is not static) */ NonStaticProperty /**/ );
				AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(NonStaticProperty is not static) */ NonStaticProperty /**/ );

				var f4 = new StatelessFunc<int>( StaticProperty );
				AttributeFuncReceiver.Accept( StaticProperty );

				var f5 = new StatelessFunc<int>( /* StatelessFuncIsnt(NonStaticMethod is not static) */ NonStaticMethod /**/ );
				AttributeFuncReceiver.Accept( /* StatelessFuncIsnt(NonStaticMethod is not static) */ NonStaticMethod /**/ );

				var f6 = new StatelessFunc<int>( StaticMethod );
				AttributeFuncReceiver.Accept( StaticMethod );

			}

		}
	}
}
