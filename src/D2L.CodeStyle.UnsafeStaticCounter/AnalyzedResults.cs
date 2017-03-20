using System;
using System.Linq;
using System.Collections.Generic;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedResults {
		public readonly int UnsafeStaticsCount;
		public readonly IDictionary<string, int> UnsafeStaticsPerCause;
		public readonly IDictionary<string, int> UnsafeStaticsPerType;
		public readonly IDictionary<string, int> UnsafeStaticsPerProject;
		public readonly IEnumerable<AnalyzedStatic> RawResults;

		public AnalyzedResults( AnalyzedStatic[] rawResults ) {
			RawResults = rawResults;
			UnsafeStaticsCount = rawResults.Length;

			UnsafeStaticsPerCause = rawResults
				.GroupBy( r => r.Cause )
				.ToDictionary( g => g.Key, Enumerable.Count );

			UnsafeStaticsPerType = rawResults
				.GroupBy( r => r.FieldOrPropType )
				.ToDictionary( g => g.Key, Enumerable.Count );

			UnsafeStaticsPerProject = rawResults
				.GroupBy( r => r.ProjectName )
				.ToDictionary( g => g.Key, Enumerable.Count );
		}
	}

	internal sealed class Aggregation {
		public readonly string Name;
		public readonly int UnsafeStaticsCount;

		public Aggregation( string name, int count ) {
			Name = name;
			UnsafeStaticsCount = count;
		}
	}
}
