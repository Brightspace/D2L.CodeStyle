using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Configs {

	internal static class BannedConfigs {

		internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Definitions = ImmutableDictionary
			.Create<string, IReadOnlyDictionary<string, string>>()
			// .Add( "GetInstance", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			.Add( "GetOrg", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase )
				.Add( "d2l.Settings.WebServerName", "WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead." )
				.Add( "d2l.System.Aws.Region", "Use IOrgAwsRegionProvider instead." )

				.Add( "Directories.Org.Content", "Use IFileSystemRootProvider.GetContentPath( orgId ) instead." )
				.Add( "Directories.Org.Shared", "Use IFileSystemRootProvider.GetSharedPath( orgId ) instead." )
			)
			// .Add( "GetOrgUnit", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			// .Add( "GetUser", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			// .Add( "GetSession", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			// .Add( "GetRole", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			// .Add( "GetUserOrgUnit", ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase ) )
			;


	}
}
