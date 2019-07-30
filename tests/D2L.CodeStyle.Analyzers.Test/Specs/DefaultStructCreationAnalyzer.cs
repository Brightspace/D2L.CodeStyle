// analyzer: D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.DefaultStructCreationAnalyzer

namespace System.Collections.Immutable {

	public static class ImmutableArray {
		public static ImmutableArray<T> Create<T>() { }
	}

	public struct ImmutableArray<T> {
		public static readonly ImmutableArray<T> Empty;
	}

}

namespace SpecTests {

	using System;
	using System.Collections.Immutable;

	public class Foo {
		public void Bar() {

			// System.Guid
			{ var x = /* DontCallDefaultStructConstructor(Guid) */ new Guid() /**/; }
			{ var x = /* DontCallDefaultStructConstructor(Guid) */ new System.Guid() /**/; }
			{ var x = new Guid( "efa31618-dced-4f3d-904f-e7424e4058fb" ); }
			{ var x = Guid.Empty; }
			{ var x = Guid.NewGuid(); }

			// System.Collections.Immmutable.ImmutableArray`1
			{ var x = /* DontCallDefaultStructConstructor(ImmutableArray<int>) */ new ImmutableArray<int>() /**/; }
			{ var x = /* DontCallDefaultStructConstructor(ImmutableArray<int>) */ new System.Collections.Immutable.ImmutableArray<int>() /**/; }
			{ var x = ImmutableArray<int>.Empty; }
			{ var x = ImmutableArray.Create<int>(); }
			{
				// constructor doesn't exist
				var x = new ImmutableArray<int>( 1 );
			}

			// D2L Guid-Backed Id Types
			{ var x = /* DontCallDefaultStructConstructor(GuidBackedIdType) */ new GuidBackedIdType() /**/; }
			{ var x = /* DontCallDefaultStructConstructor(GuidBackedIdType) */ new SpecTests.GuidBackedIdType() /**/; }
			{ var x = GuidBackedIdType.GenerateNew(); }

			// Things we don't know about
			{ var x = new SomeOtherStruct(); }

		}
	}

	public readonly partial struct GuidBackedIdType {

		public static GuidBackedIdType GenerateNew() {}

	}

	public readonly struct SomeOtherStruct {}

}
