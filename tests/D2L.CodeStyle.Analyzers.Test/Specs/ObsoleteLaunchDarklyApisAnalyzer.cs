// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly.ObsoleteLaunchDarklyApisAnalyzer

namespace D2L.LP.LaunchDarkly {
	public interface ILaunchDarklyClient {
		void Flush();
	}
}

namespace D2L.LP.LaunchDarkly.FeatureFlagging {
	public interface IFeature { }
}

namespace D2L.Core.Users.Domain.Feature {
	using D2L.LP.LaunchDarkly.FeatureFlagging;

	public sealed class PreferredNamesFeatureLD : IFeature { }
}

namespace SpecTests {
	using D2L.LP.LaunchDarkly.FeatureFlagging;

	public sealed class SpyCamFeature :/* ObsoleteLaunchDarklyFramework */ IFeature /**/{ }

	public struct SneakyStructFeature :/* ObsoleteLaunchDarklyFramework */ IFeature /**/{ }
}

namespace MissingNamespaceUsing {

	public sealed class IncompleteFeature : IFeature { }
}

namespace UnreleatedInterfaces {

	public sealed class IncompleteFeature : System.ICloneable { }
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
