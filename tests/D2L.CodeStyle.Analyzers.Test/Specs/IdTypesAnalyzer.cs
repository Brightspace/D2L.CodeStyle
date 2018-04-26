// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.IdTypesAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs {
	public class Foo {
		public void Foo() {
			long orgId = 1;
			long orgUnitId = 2;
			long userId = 3;
			long foo = 4;

			Bar( orgId, orgUnitId, userId );
			Bar( orgId, orgId, userId );
			Bar( foo, foo, foo );
			Baz( foo, foo, foo );
			Baz( orgId, orgUnitId, userId );
			Bar( /* IdTypeParameterMismatch */ orgUnitId /**/, orgUnitId, userId );
			Bar( orgId, orgUnitId, /* IdTypeParameterMismatch */ orgUnitId /**/ );
			Bar( orgId, orgUnitId, /* IdTypeParameterMismatch */ orgId /**/ );
			Bar( /* IdTypeParameterMismatch */ userId /**/, orgUnitId, userId );
			Bar( orgId, /* IdTypeParameterMismatch */ userId /**/, userId );
		}

		public void Bar( long orgId, long orgUnitId, long userId ) { }
		public void Baz( long foo, long bar, long baz );
	}
}
