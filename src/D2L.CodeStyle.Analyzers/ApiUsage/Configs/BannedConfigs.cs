using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Configs {

	internal static class BannedConfigs {

		internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Definitions = ImmutableDictionary
			.Create<string, IReadOnlyDictionary<string, string>>()
			// .Add( "GetInstance", ImmutableDictionary.Create<string, string>() )
			.Add( "GetOrg", ImmutableDictionary.Create<string, string>()
				.Add( "d2l.Settings.WebServerName", "WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead." )
			)
			// .Add( "GetOrgUnit", ImmutableDictionary.Create<string, string>() )
			// .Add( "GetUser", ImmutableDictionary.Create<string, string>() )
			// .Add( "GetSession", ImmutableDictionary.Create<string, string>() )
			// .Add( "GetRole", ImmutableDictionary.Create<string, string>() )
			// .Add( "GetUserOrgUnit", ImmutableDictionary.Create<string, string>() )
			;


	}
}
