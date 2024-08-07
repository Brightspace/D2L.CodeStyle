// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventTypesAnalyzer, D2L.CodeStyle.Analyzers

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
	public abstract class/* EventTypeNotSealed(Tests.AbstractEvent) */ AbstractEvent /**/{ }

	[Event]
	[Immutable]
	public class/* EventTypeNotSealed(Tests.NonSealedEvent) */ NonSealedEvent /**/{ }

	[Event]
	[Immutable]
	public sealed class ImmutableEvent { }

	[System.Diagnostics.Tracing.Event]
	public sealed class WrongEventAttribute { }

	[Wacky.Event]
	public sealed class InvalidAttributeType { }
}
