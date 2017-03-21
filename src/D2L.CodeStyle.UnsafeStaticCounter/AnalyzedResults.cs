using System.Linq;
using System.Collections.Generic;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedResults {
		public readonly int UnsafeStaticsCount;
		public readonly IReadOnlyCollection<string> UnanalyzedProjects;
		public readonly IDictionary<string, int> UnsafeStaticsPerCause;
		public readonly IDictionary<string, int> UnsafeStaticsPerType;
		public readonly IDictionary<string, int> UnsafeStaticsPerProject;
		public readonly IReadOnlyCollection<AnalyzedStatic> RawResults;

		public AnalyzedResults( AnalyzedProject[] projects ) {
			// grab all results
			RawResults = projects.SelectMany( p => p.RawResults ).ToArray();
			UnsafeStaticsCount = RawResults.Count;

			// grab list of unanalyzed projects
			UnanalyzedProjects = projects
				.Where( p => !p.IsAnalyzed )
				.Select( p => p.Name )
				.ToArray();

			// group results per project
			UnsafeStaticsPerProject = RawResults
				.GroupBy( r => r.ProjectName )
				.ToDictionary( g => g.Key, Enumerable.Count );

			// group results by cause
			UnsafeStaticsPerCause = RawResults
				.GroupBy( r => r.Cause )
				.ToDictionary( g => g.Key, Enumerable.Count );

			// group results by type
			UnsafeStaticsPerType = RawResults
				.GroupBy( r => r.FieldOrPropType )
				.ToDictionary( g => g.Key, Enumerable.Count );

		}
	}

	internal sealed class AnalyzedProject {
		public readonly string Name;
		public readonly bool IsAnalyzed;
		public readonly AnalyzedStatic[] RawResults;

		public AnalyzedProject( 
			string name, 
			bool isAnalyzed, 
			AnalyzedStatic[] rawResults 
		) {
			Name = name;
			IsAnalyzed = isAnalyzed;
			RawResults = rawResults;
		}
	}
}
