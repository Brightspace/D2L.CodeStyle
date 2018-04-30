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


		struct IdTypeA {

			private readonly long m_value;

			public IdTypeA( long m_value ) {
				m_value = m_value;
			}

			public implicit operator long( IdTypeA id ) {
				return id.m_value;
			}

			public implicit operator IdTypeA( long value ) {
				return new IdTypeA( value );
			}
		}

		struct IdTypeB {

			private readonly long m_value;

			public IdTypeB( long m_value ) {
				m_value = m_value;
			}

			public implicit operator long( IdTypeB id ) {
				return id.m_value;
			}

			public implicit operator IdTypeB( long value ) {
				return new IdTypeB( value );
			}
		}

		void Foo( IdTypeA orgId, IdTypeB userId, string foo ) {
			long orgIdLong = 1234;
			long userIdLong = 5678;

			Foo( orgIdLong, userIdLong );
			Foo(
				/* LikelyArgumentMismatch(userIdLong,userId,orgId) */ userIdLong /**/,
				/* LikelyArgumentMismatch(orgIdLong,orgId,userId) */ orgIdLong /**/,
				foo
			);
		}

	}
}
