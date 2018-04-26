// analyzer: D2L.CodeStyle.Analyzers.Language.LikelyArgumentMismatchAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs {
	public class LikelyArgumentMismatchAnalyzer {

		void Foo( long userId, long orgId, long orgUnitId ) {
			Foo(
				/* LikelyArgumentMismatch(orgId,orgId,userId) */ orgId /**/,
				/* LikelyArgumentMismatch(orgUnitId,orgUnitId,orgId) */ orgUnitId /**/,
				/* LikelyArgumentMismatch(userId,userId,orgUnitId) */ userId /**/
			);
		}

	}
}
