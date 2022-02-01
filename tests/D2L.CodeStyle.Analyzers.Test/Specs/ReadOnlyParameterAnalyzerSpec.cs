// analyzer: D2L.CodeStyle.Analyzers.Immutability.ReadOnlyParameterAnalyzer

using System;

namespace D2L.CodeStyle.Annotations {

	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public class ReadOnlyAttribute : Attribute { }
}

namespace SpecTests {

	using D2L.CodeStyle.Annotations;

	internal sealed class ReadOnlyAttributeUsages {

		void Unused( [ReadOnly] int foo ) {}

		void OnlyRead( [ReadOnly] int foo ) {
			int bar = foo;
		}

		void PassedByValue( [ReadOnly] int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( [ReadOnly] int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( [ReadOnly] int foo ) {
			return foo;
		}

		void WrittenToInBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			() => { foo = 1; };
		}

		void PassedToRef( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnly] int foo /**/ ) {
			RefParameter( ref foo );
		}

		void RefParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnly] ref int foo /**/ ) { }
		void InParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnly] in int foo /**/ ) { }

		internal class C {
			C( [ReadOnly] int foo ) { }
		}

		internal interface I {
			void Foo( [ReadOnly] int foo );
		}

	}

	internal sealed class SubclassAttributeUsages {

		[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
		public sealed class ReadOnlySubclassAttribute : ReadOnlyAttribute { }

		void Unused( [ReadOnlySubclass] int foo ) { }

		void OnlyRead( [ReadOnlySubclass] int foo ) {
			int bar = foo;
		}

		void PassedByValue( [ReadOnlySubclass] int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( [ReadOnlySubclass] int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( [ReadOnlySubclass] int foo ) {
			return foo;
		}

		void WrittenToInBody( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			() => { foo = 1; };
		}

		void PassedToRef( /* ReadOnlyParameterIsnt(is assigned to and/or passed by reference) */ [ReadOnlySubclass] int foo /**/ ) {
			RefParameter( ref foo );
		}

		void RefParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnlySubclass] ref int foo /**/ ) { }
		void InParameter( /* ReadOnlyParameterIsnt(is an in/ref/out parameter) */ [ReadOnlySubclass] in int foo /**/ ) { }

		internal class C {
			C( [ReadOnlySubclass] int foo ) { }
		}

		internal interface I {
			void Foo( [ReadOnlySubclass] int foo );
		}

	}

	internal sealed class NonReadOnlyThings {

		void Unused( int foo ) { }

		void OnlyRead( int foo ) {
			int bar = foo;
		}

		void PassedByValue( int foo ) {
			WrittenToInBody( foo );
		}

		void PassedToIn( int foo ) {
			InParameter( foo );
		}

		ref readonly int ReadonlyRefReturn( int foo ) {
			return foo;
		}

		void WrittenToInBody( int foo ) {
			foo = 1;
		}

		void WrittenToInInlineFunc( int foo ) {
			void Helper() { foo = 1; }
		}

		void WrittenToInLambda( int foo ) {
			() => { foo = 1; };
		}

		void PassedToRef( int foo ) {
			RefParameter( ref foo );
		}

		void RefParameter( ref int foo ) { }
		void InParameter( in int foo ) { }

	}
}
