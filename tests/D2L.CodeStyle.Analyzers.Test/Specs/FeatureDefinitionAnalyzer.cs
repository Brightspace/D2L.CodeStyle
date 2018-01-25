// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly.FeatureDefinitionAnalyzer

namespace D2L.LP.LaunchDarkly {
	public abstract class FeatureDefinition<TValue> { }
}

namespace SpecTests {
	using System;
	using D2L.LP.LaunchDarkly;

	public sealed class ObjectDefinition :/* InvalidLaunchDarklyFeatureDefinition(long) */ FeatureDefinition<long> /**/{ }
	public sealed class ObjectDefinition :/* InvalidLaunchDarklyFeatureDefinition(System.TimeSpan) */ FeatureDefinition<TimeSpan> /**/{ }

	public sealed class BoolDefinition : FeatureDefinition<bool> { }
	public sealed class FloatDefinition : FeatureDefinition<float> { }
	public sealed class IntDefinition : FeatureDefinition<int> { }
	public sealed class StringDefinition : FeatureDefinition<string> { }

	public sealed class BoolDefinition : FeatureDefinition<Boolean> { }
	public sealed class FloatDefinition : FeatureDefinition<Single> { }
	public sealed class IntDefinition : FeatureDefinition<Int32> { }
	public sealed class StringDefinition : FeatureDefinition<String> { }

	public sealed class BoolDefinition : FeatureDefinition<System.Boolean> { }
	public sealed class FloatDefinition : FeatureDefinition<System.Single> { }
	public sealed class IntDefinition : FeatureDefinition<System.Int32> { }
	public sealed class StringDefinition : FeatureDefinition<System.String> { }
}

namespace MissingLaunchDarklyUsingStatement {

	public sealed class ObjectDefinition : FeatureDefinition<System.TimeSpan> { }
}

namespace UnresolvedValueType {
	using D2L.LP.LaunchDarkly;

	public sealed class ObjectDefinition : FeatureDefinition<YMCA> { }
}