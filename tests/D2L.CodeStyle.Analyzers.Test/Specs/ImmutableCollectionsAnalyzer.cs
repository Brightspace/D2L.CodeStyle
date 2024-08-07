// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.SystemCollectionsImmutable.ImmutableCollectionsAnalyzer, D2L.CodeStyle.Analyzers

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.Specs {
	class ImmutableCollectionsAnalyzer {
		public object m_good = new object();
		public int[] m_good2 = new int[] { 1, 2, 3 };
		public IEnumerable<int> m_good3 = new List<int> { };
		public IEnumerable<int> m_good4 = ImmutableArray.Create( 1, 2, 3 );
		public ImmutableArray<int> m_good5 = ImmutableArray<int>.Empty;
		public int[] m_good6 = new int[0](); // FYI: not an ObjectionCreationSyntax - there is a distinct ArrayCreationSyntax

		public ImmutableArray<int> m_neutral = new ImmutableArray<int>( 1 ); // we don't need to report for this, there is no such constructor.
		public ImmutableArray<int> m_neutral2 = new ImmutableArray<int>( new int[] { 1, 2, 3 } ); // this constructor is internal

		public IEnumerable<int> m_bad = /* DontUseImmutableArrayConstructor */ new ImmutableArray<int> { 1, 2, 3 } /**/;
		public ImmutableArray<int> m_bad2 = /* DontUseImmutableArrayConstructor */ new ImmutableArray<int> { 1, 2, 3 } /**/;
		public ImmutableArray<int> m_bad2 = /* DontUseImmutableArrayConstructor */ new ImmutableArray<int> {} /**/;
		public ImmutableArray<int> m_bad2 = /* DontUseImmutableArrayConstructor */ new ImmutableArray<int>() /**/;

		// not handled, but not likely?
		public ImmutableArray<int> m_notCurrentlyHandled = default( ImmutableArray<int> );

		public static void SomeMethod() {
			var bad = /* DontUseImmutableArrayConstructor */ new ImmutableArray<int>() /**/;
			var good = ImmutableArray.Create( 1, 2, 3 );
			var good2 = new object();
		}

		public static T GenericMethod<T>() where T : new() {
			return new T();
		}

		public void SomeOtherMethod() {
			// We don't catch this. In general doing so would be too
			// complicated for an analyzer: we'd need to look up the defn of
			// GenericMethod, do data-flow analysis etc.
			var whoops = GenericMethod<ImmutableArray<int>>();
		}
	}
}
