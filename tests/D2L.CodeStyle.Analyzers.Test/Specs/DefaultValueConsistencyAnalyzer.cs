// analyzer: D2L.CodeStyle.Analyzers.Language.DefaultValueConsistencyAnalyzer

namespace ThingThatShouldntCrash {
	public void FunctionOutOfPlace( int arg = 3 );

	// This has a BaseListSyntax but should be ignored
	enum Foo : int {
		A, B, C
	}

	public class EmptyBase { }

	public class ThingWithMysteriousOverride : EmptyBase {
		public override void Bar( int abc ) { }
	}
}

namespace ClassInheritance {

	public class ClassWithNoInterestingOverrides {
		public int Foo();
		public void Bar( int x = 3 );
		public override string ToString() { }
		public override int GetHashCode() { }
		// We don't worry about Equals
		public override bool Equals( object obj = null ) { }
	}

	public abstract class BaseClass {
		public abstract void Foo();
		public abstract void Bar( int x = 3 );
		public abstract void Baz( float x, string y = "hello" );
	}

	public sealed class GoodImpl : BaseClass {
		public override void Foo() { }
		public override void Bar( int x = 3 ) { }
		public override void Baz( float x, string y = "hello" ) { }
	}

	public sealed class InconsistentImpl : BaseClass {
		public override void Foo() { }

		public override void Bar(
			/* DefaultValuesInOverridesShouldBeConsistent(x, 123123, 3, BaseClass) */ int x = 123123 /**/
		) { }

		public override void Baz(
			float x,
			/* DefaultValuesInOverridesShouldBeConsistent(y, null, "hello", BaseClass) */ string y = null /**/
		) { }
	}

	public sealed class MissingImpl : BaseClass {
		public override void Foo() { }

		public override void Bar(
			/* IncludeDefaultValueInOverrideForReadability(x, BaseClass) */ int x /**/
		) { }

		public override void Baz(
			float x,
			/* IncludeDefaultValueInOverrideForReadability(y, BaseClass) */ string y /**/
		) { }
	}

	public sealed class ImplWithNewDefaults : BaseClass {
		public override void Foo() { }
		public override void Bar( int x = 3 ) { }
		public override void Baz(
			/* DontIntroduceNewDefaultValuesInOverrides(x, BaseClass) */ float x = 3.14 /**/,
			string y = "hello"
		) { }
	}

	public abstract class OtherBase {
		public abstract void Foo( int multi = 0, int param = 1, int method = 2 );
	}

	public sealed class MultiInconsistentMethodImpl : OtherBase {
		public override void Foo(
			/* DefaultValuesInOverridesShouldBeConsistent(multi, 3, 0, OtherBase) */ int multi = 3 /**/,
			/* DefaultValuesInOverridesShouldBeConsistent(param, 4, 1, OtherBase) */ int param = 4 /**/,
			/* DefaultValuesInOverridesShouldBeConsistent(method, 5, 2, OtherBase) */ int method = 5 /**/
		) {}
	}

}

namespace Interfaces {
	public interface IFoo {
		void Foo();
		void Bar( int x = 3 );
		void Baz( float x, string y = null );
	}

	public struct GoodImplicitImpl : IFoo {
		public void Foo() { }
		public void Bar( int x = 3 ) { }
		public void Baz( float x, string y = null ) { }
	}

	public struct ExplicitImplIsAlwaysGood : IFoo {
		void IFoo.Foo() { }
		void IFoo.Bar( int x ) { }
		void IFoo.Baz( float x, string y ) { }
	}

	public struct InconsistentImpl : IFoo {
		public void Foo() { }

		public void Bar(
			/* DefaultValuesInOverridesShouldBeConsistent(x, 123123, 3, IFoo) */ int x = 123123 /**/
		) { }

		public void Baz(
			float x,
			/* DefaultValuesInOverridesShouldBeConsistent(y, "bye", null, IFoo) */ string y = "bye" /**/
		) { }
	}

	public sealed class MissingImpl : IFoo {
		public void Foo() { }

		public void Bar(
			/* IncludeDefaultValueInOverrideForReadability(x, IFoo) */ int x /**/
		) { }

		public void Baz(
			float x,
			/* IncludeDefaultValueInOverrideForReadability(y, IFoo) */ string y /**/
		) { }
	}

	public sealed class ImplWithNewDefaults : IFoo {
		public void Foo() { }
		public void Bar( int x = 3 ) { }
		public void Baz(
			/* DontIntroduceNewDefaultValuesInOverrides(x, IFoo) */ float x = 3.14 /**/,
			string y = null
		) { }
	}
}

namespace MultipleInterfaces {
	public interface IFoo {
		void Hello( int x = 111 );
	}

	public interface IBar {
		void Hello( int x = 222 );
	}

	public sealed class ExplicitDude : IFoo, IBar {
		void IFoo.Hello( int x ) { }
		void IBar.Hello( int x ) { }
	}

	// Not advocating this
	public sealed class HalfExplicitDudeButConsistent : IFoo, IBar {
		void IFoo.Hello( int x ) { }
		void Hello( int x = 222 ) { }
	}

	public sealed class HalfExplicitDudeInconsistent : IFoo, IBar {
		void IFoo.Hello( int x ) { }

		public void Hello(
			/* DefaultValuesInOverridesShouldBeConsistent(x, 333, 222, IBar) */ int x = 333 /**/
		) { }
	}

	public sealed class HalfExplicitDudeMissingDefault : IFoo, IBar {
		void IFoo.Hello( int x ) { }

		public void Hello(
			/* IncludeDefaultValueInOverrideForReadability(x, IBar) */ int x /**/
		) { }
	}
}

namespace InterfaceFromBaseClass {
	public interface IFoo {
		void Fizz( int x = 111 );
		void Buzz( int y = 222 );
		void FizzBuzz( int s );
	}

	public class BaseClass {
		public void Fizz( int x );
		public void Buzz( int y = 333 );
		public void FizzBuzz( int s = 444 );
	}

	public class InterhitsInterfaceImplementation : /*
		IncludeDefaultValueInOverrideForReadability(x, IFoo)
		| DefaultValuesInOverridesShouldBeConsistent(y, 333, 222, IFoo)
		| DontIntroduceNewDefaultValuesInOverrides(s, IFoo)
		*/ BaseClass /**/, IFoo { }
}

namespace NewModifier {
	interface IBaseInterface {
		void GetStuff( int count = 99 );
	}

	interface IMiddleInterface : IBaseInterface {
		// not advocating this in any way, and should probably be its own error
		new void GetStuff( int count = 55 );
	}

	class SomeClass : IMiddleInterface {
		// Gets called via SomeClass and IBaseInterface
		public void GetStuff( int count = 99 ) {}

		void IMiddleInterface.GetStuff( int count ) {}
	}

	class SomeClass2 : IMiddleInterface {
		public void GetStuff(
			/* DefaultValuesInOverridesShouldBeConsistent(count, 123, 99, IBaseInterface) */ int count = 123 /**/
		) { }
		void IMiddleInterface.GetStuff( int count ) {}
	}
}
