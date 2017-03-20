using System;
using System.Collections.Generic;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedResults {
		public readonly int UnsafeStaticsCount;
		public readonly IEnumerable<Aggregation> UnsafeStaticsPerCause;
		public readonly IEnumerable<Aggregation> UnsafeStaticsPerType;
		public readonly IEnumerable<Aggregation> UnsafeStaticsPerProject;
		public readonly IEnumerable<AnalyzedStatic> RawResults;

		public AnalyzedResults( AnalyzedStatic[] rawResults ) {
			RawResults = rawResults;

			UnsafeStaticsCount = rawResults.Length;

			var unsafeStaticsPerCause = new Dictionary<string, Aggregation>();
			var unsafeStaticsPerProject = new Dictionary<string, Aggregation>();
			var unsafeStaticsPerType = new Dictionary<string, Aggregation>();

			foreach( var result in rawResults ) {

				// increment per project
				var project = unsafeStaticsPerProject.GetOrAdd(
						result.ProjectName,
						() => new Aggregation( result.ProjectName )
					);
				project.UnsafeStaticsCount++;

				// increment per type
				var analyzedType = unsafeStaticsPerType.GetOrAdd(
					result.FieldOrPropType,
					() => new Aggregation( result.FieldOrPropType )
				);
				analyzedType.UnsafeStaticsCount++;

				// increment per cause
				var analyzedCause = unsafeStaticsPerCause.GetOrAdd(
					result.Cause,
					() => new Aggregation( result.Cause )
				);
				analyzedCause.UnsafeStaticsCount++;

			}

			UnsafeStaticsPerCause = unsafeStaticsPerCause.Values;
			UnsafeStaticsPerProject = unsafeStaticsPerProject.Values;
			UnsafeStaticsPerType = unsafeStaticsPerType.Values;
		}
	}

	internal sealed class Aggregation {
		public readonly string Name;
		public int UnsafeStaticsCount;

		public Aggregation( string name ) {
			Name = name;
		}
	}
}
