// analyzer: D2L.CodeStyle.Analyzers.LaunchDarkly.ObsoleteLaunchDarklyApisAnalyzer

namespace D2L.LP.LaunchDarkly.FeatureFlagging {
	public interface IFeature { }
}

namespace D2L.Integration.ParentPortal {
	using D2L.LP.LaunchDarkly.FeatureFlagging;

	public sealed class AppDynamicsFeature : IFeature { }
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
