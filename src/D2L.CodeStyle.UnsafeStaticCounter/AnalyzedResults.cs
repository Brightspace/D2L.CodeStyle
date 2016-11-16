using System.Collections.Generic;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedResults {
		public readonly int UnsafeStaticsCount;
		public readonly int UnsafeNonReadonlyStaticsCount;
		public readonly IEnumerable<AnalyzedType> UnsafeStaticsPerType;
		public readonly IEnumerable<AnalyzedProject> UnsafeStaticsPerProject;
		public readonly IEnumerable<AnalyzedStatic> RawResults;

		public AnalyzedResults( AnalyzedStatic[] rawResults ) {
			RawResults = rawResults;

			UnsafeStaticsCount = rawResults.Length;

			var unsafeStaticsPerProject = new Dictionary<string, AnalyzedProject>();
			var unsafeStaticsPerType = new Dictionary<string, AnalyzedType>();

			foreach( var result in rawResults ) {

				// increment per project
				if( !unsafeStaticsPerProject.ContainsKey( result.ProjectName ) ) {
					unsafeStaticsPerProject[result.ProjectName] = new AnalyzedProject( result.ProjectName );
				}
				unsafeStaticsPerProject[result.ProjectName].UnsafeStaticsCount++;


				// increment per-type
				if( result.FieldOrPropType != null ) {
					if( !unsafeStaticsPerType.ContainsKey( result.FieldOrPropType ) ) {
						unsafeStaticsPerType[result.FieldOrPropType] = new AnalyzedType( result.FieldOrPropType );
					}
					unsafeStaticsPerType[result.FieldOrPropType].UnsafeStaticsCount++;
				}
			}

			// move `it` to readonly count
			if( unsafeStaticsPerType.ContainsKey( "it" ) ) {
				UnsafeNonReadonlyStaticsCount = unsafeStaticsPerType["it"].UnsafeStaticsCount;
				unsafeStaticsPerType.Remove( "it" );
			}

			UnsafeStaticsPerProject = unsafeStaticsPerProject.Values;
			UnsafeStaticsPerType = unsafeStaticsPerType.Values;
		}
	}

	internal sealed class AnalyzedProject {
		public readonly string Name;
		public int UnsafeStaticsCount;

		public AnalyzedProject( string projectName ) {
			Name = projectName;
		}
	}
	internal sealed class AnalyzedType {
		public readonly string Name;
		public int UnsafeStaticsCount;

		public AnalyzedType( string typeName ) {
			Name = typeName;
		}
	}
}
