// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly.ObsoleteLaunchDarklyApisAnalyzer

namespace D2L.LP.LaunchDarkly {
	public interface ILaunchDarklyClient {
		void Flush();
	}
}

namespace BannedMethods {
	using D2L.LP.LaunchDarkly;

	public sealed class NonLegacyILaunchDarklyClientConsumers {

		public void Method( ILaunchDarklyClient client ) {
			/* ObsoleteILaunchDarklyClientClient */	client.Flush();/**/
		}
	}
}

namespace D2L.ClassStream.FeatureFlag {
	using D2L.LP.LaunchDarkly;

	public sealed class ClassStreamFeatureToggle {

		public void IsOneDrivePickerEnabled( ILaunchDarklyClient client ) {
			client.Flush();
		}
	}
}
