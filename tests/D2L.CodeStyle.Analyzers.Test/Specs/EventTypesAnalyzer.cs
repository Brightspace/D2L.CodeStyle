
// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventTypesAnalyzer

namespace D2L.CodeStyle.Annotations {

	using System;

	public static class Objects {
		public sealed class ImmutableAttribute : Attribute {}
	}
}

namespace D2L.LP.Distributed.Events.Domain {

	using System;

	[AttributeUsage( validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public sealed class EventAttribute : Attribute { }
}

namespace Tests {

	using System;
	using D2L.LP.Distributed.Events.Domain;
	using static D2L.CodeStyle.Annotations.Objects;

	[Event]
	public sealed class/* EventTypeMissingImmutableAttribute(Tests.MutableEvent) */ MutableEvent /**/{ }

	[Event]
	[Immutable]
	public sealed class ImmutableEvent { }

	[System.Diagnostics.Tracing.Event]
	public sealed class WrongEventAttribute { }

	[Wacky.Event]
	public sealed class InvalidAttributeType { }
}
