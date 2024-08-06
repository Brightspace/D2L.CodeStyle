// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventHandlerLoaderTypesAnalyzer, D2L.CodeStyle.Analyzers

namespace D2L.LP.Distributed.Events.Domain {

	using System;

	[AttributeUsage( validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public sealed class EventAttribute : Attribute { }
}

namespace D2L.LP.Distributed.Events.Handlers {

	using System;

	[AttributeUsage( validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public sealed class EventHandlerAttribute : Attribute {
		public EventHandlerAttribute( string id ) { }
	}

	public interface IEventHandlerRegistry {
		void RegisterEventHandler<TEvent, THandler>() where THandler : IEventHandler<TEvent>;
		void RegisterOrgEventHandler<TEvent, THandler>() where THandler : IOrgEventHandler<TEvent>;
	}

	public interface IEventHandler<T> { }
	public interface IOrgEventHandler<T> { }
}

namespace Tests {

	using System;
	using D2L.LP.Distributed.Events.Domain;
	using D2L.LP.Distributed.Events.Handlers;

	[Event]
	public sealed class JumpEvent { }

	[EventHandler( "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA" )]
	public sealed class AttributedJumpEventHandler : IEventHandler<JumpEvent> { }

	[EventHandler( "BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB" )]
	public sealed class AttributedJumpOrgEventHandler : IOrgEventHandler<JumpEvent> { }

	public sealed class NonAttributedJumpEventHandler : IEventHandler<JumpEvent> { }
	public sealed class NonAttributedJumpOrgEventHandler : IOrgEventHandler<JumpEvent> { }

	public sealed class SkipEvent { }

	[EventHandler( "CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC" )]
	public sealed class AttributedSkipEventHandler : IEventHandler<SkipEvent> { }

	[EventHandler( "DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD" )]
	public sealed class AttributedSkipOrgEventHandler : IOrgEventHandler<SkipEvent> { }

	public sealed class WithEventAttribute {

		public void Load( IEventHandlerRegistry registry ) {

			registry.RegisterEventHandler<JumpEvent, AttributedJumpEventHandler>();
			registry.RegisterOrgEventHandler<JumpEvent, AttributedJumpOrgEventHandler>();
		}
	}

	public sealed class WithoutEventAttribute {

		public void Load( IEventHandlerRegistry registry ) {

			/* EventTypeMissingEventAttribute(Tests.SkipEvent) */	registry.RegisterEventHandler<SkipEvent, AttributedSkipEventHandler>() /**/;
			/* EventTypeMissingEventAttribute(Tests.SkipEvent) */	registry.RegisterOrgEventHandler<SkipEvent, AttributedSkipOrgEventHandler>() /**/;
		}
	}
	
	public sealed class WithoutEventHandlerAttribute {

		public void Load( IEventHandlerRegistry registry ) {

			/* EventHandlerTypeMissingEventAttribute(Tests.NonAttributedJumpEventHandler) */	registry.RegisterEventHandler<JumpEvent, NonAttributedJumpEventHandler>() /**/;
			/* EventHandlerTypeMissingEventAttribute(Tests.NonAttributedJumpOrgEventHandler) */	registry.RegisterOrgEventHandler<JumpEvent, NonAttributedJumpOrgEventHandler>() /**/;
		}
	}
}
