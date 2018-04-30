// analyzer: D2L.CodeStyle.Analyzers.Language.ParamNamesShouldMatchInterfaceAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs {
	public interface IFoo {
		int Foo( int a, int b, int c );
		string Foo( int a, int b, int c );
		string Foo( string g, string h, string i );

		int Bar( int d, int e, int f );

		void Foo();
	}

	public class GoodExplicit : IFoo {
		int IFoo.Foo( int a, int b, int c ) { return 0; }
		string IFoo.Foo( int a, int b, int c ) { return null; }
		string IFoo.Foo( string g, string h, string i ) { return null; }

		int IFoo.Bar( int d, int e, int f ) { return 0; }

		void IFoo.Foo() { }
	}

	public class GoodImplict : IFoo {
		public int Foo( int a, int b, int c ) { return 0; }
		public string Foo( int a, int b, int c ) { return null; }
		public string Foo( string g, string h, string i ) { return null; }

		public int Bar( int d, int e, int f ) { return 0; }

		public void Foo() { }
	}

	public class BadExplicit : IFoo {
		int IFoo.Foo( /* InterfaceImplementationParamNameMismatch(a,b) */ int b /**/, /* InterfaceImplementationParamNameMismatch(b,a) */ int a /**/, int c ) { return 0; }
		string IFoo.Foo( int a, int b, int c ) { return null; }
		string IFoo.Foo( string g, string h, /* InterfaceImplementationParamNameMismatch(i,x) */ string x /**/ ) { return null; }

		int IFoo.Bar( int d, int e, int f ) { return 0; }

		void IFoo.Foo() { }
	}

	public class BadImplicit : IFoo {
		public int Foo( /* InterfaceImplementationParamNameMismatch(a,b) */ int b /**/, /* InterfaceImplementationParamNameMismatch(b,a) */ int a /**/, int c ) { return 0; }
		public string Foo( int a, int b, int c ) { return null; }
		public string Foo( string g, string h, /* InterfaceImplementationParamNameMismatch(i,x) */ string x /**/ ) { return null; }

		public int Bar( int d, int e, int f ) { return 0; }

		public void Foo() { }
	}

	public class BadBase {
		public int Foo( /* InterfaceImplementationParamNameMismatch(a,b) */ int b /**/, /* InterfaceImplementationParamNameMismatch(b,a) */ int a /**/, int c ) { return 0; }
		public string Foo( int a, int b, int c ) { return null; }
		public string Foo( string g, string h, /* InterfaceImplementationParamNameMismatch(i,x) */ string x /**/ ) { return null; }

		public int Bar( int d, int e, int f ) { return 0; }

		public void Foo() { }
	}

	public class BadSub : BadBase, IFoo { }

	public class UnfinishedImplementationDoesntThrow : IFoo { }
}
