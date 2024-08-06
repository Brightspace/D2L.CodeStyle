// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Configs.ConfigViewerAnalyzer, D2L.CodeStyle.Analyzers

namespace D2L.LP.Configuration.Config.Domain {
	public interface IConfigViewer {
		T GetInstance<T>( string configName );
		T GetOrg<T>( long orgId, string configName );
		T GetOrgUnit<T>( long orgId, long orgUnitId, string configName );
		T GetUser<T>( long orgId, long userId, string configName );
		T GetSession<T>( long orgId, long sessionId, string configName );
		T GetRole<T>( long orgId, long roleId, string configName );
		T GetUserOrgUnit<T>( long orgId, long orgUnitId, long userId, string configName );
	}
}

namespace D2L.CodeStyle.Analyzers.Specs {

	using D2L.LP.Configuration.Config.Domain;

	internal sealed class Foo {

		private readonly IConfigViewer m_configViewer;

		public void BannedConfigs() {

			m_configViewer.GetOrg<string>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l.Settings.WebServerName" /**/
			);

			m_configViewer.GetOrg<string>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l.settings.webservername" /**/
			);

			m_configViewer.GetOrg<string>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l.seTTings.webserVERnamE" /**/
			);

			const string WebServerNameConfig = "d2l.Settings.WebServerName";
			m_configViewer.GetOrg<string>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ WebServerNameConfig /**/
			);

			m_configViewer.GetOrg<string>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l." + "Settings." + "WebServerName" /**/
			);

			m_configViewer.GetOrg<string>(
				configName: /* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l.Settings.WebServerName" /**/,
				orgId: 123
			);

			// <T> isn't considered
			m_configViewer.GetOrg<int>(
				123,
				/* BannedConfig(d2l.Settings.WebServerName, WebServerName is being moved to Hiera data. Use IUrlFormatter or IWebServerNameProvider instead.) */ "d2l.Settings.WebServerName" /**/
			);
		}

		public void OkayConfigs() {

			// Only marked on the expected function
			m_configViewer.GetInstance<string>( "d2l.Settings.WebServerName" );

			// Other configs names are okay
			m_configViewer.GetOrg<string>( 123, "foo.Bar.Baz" );

			// Null config name really shouldn't be ok but the analyzer doesn't currently report about it
			m_configViewer.GetOrg<string>( 123, null );
		}
	}
}
