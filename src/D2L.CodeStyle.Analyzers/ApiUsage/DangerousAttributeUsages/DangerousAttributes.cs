using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousAttributeUsages {

	internal static class DangerousAttributes {

		internal static readonly IReadOnlyList<string> Definitions =
			ImmutableList.Create<string>().Add( "JsonParamBinder" );

	}

}
