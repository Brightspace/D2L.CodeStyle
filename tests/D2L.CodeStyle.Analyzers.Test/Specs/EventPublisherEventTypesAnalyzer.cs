
// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventPublisherEventTypesAnalyzer

namespace D2L.LP.Distributed.Events.Domain {

	using System;
	using System.Collections.Generic;

	[AttributeUsage( validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public sealed class EventAttribute : Attribute {
	}

	public interface IEventPublisher {
		void Publish<T>( long orgId, T @event );
		void Publish<T>( long orgId, T @event, DateTime publishDateTime );
		void PublishMany<T>( long orgId, IEnumerable<T> events );
		void PublishMany<T>( long orgId, IEnumerable<T> events, DateTime publishDateTime );
		void ObsoleteAndUnboundedPublishMany<T>( long orgId, IEnumerable<T> events );
		void ObsoleteAndUnboundedPublishMany<T>( long orgId, IEnumerable<T> events, DateTime publishDateTime );
	}

	public interface IEventNotifier {
		void Publish<T>( T @event );
		void PublishMany<T>( IEnumerable<T> events );
	}
}

namespace D2L.LP.Distributed.Events.Processing.Domain {

	public sealed class RefiredEventEnvelope {
		public static RefiredEventEnvelope Create<T>( long orgId, T @event ) {
			return new RefiredEventEnvelope();
		}
	}
}

namespace Tests {

	using System;
	using D2L.LP.Distributed.Events.Domain;
	using D2L.LP.Distributed.Events.Processing.Domain;

	[Event]
	public sealed class GoodEvent { }

	public sealed class BadEvent { }

	public sealed class WithEventAttribute {

		public void IEventNotifier( IEventNotifier notifier ) {

			GoodEvent @event = new GoodEvent();
			notifier.Publish( @event );
		
			GoodEvent[] events = new[] { @event  };
			notifier.PublishMany( events );
		}

		public void IEventPublisher( IEventPublisher publisher ) {

			GoodEvent @event = new GoodEvent();
			publisher.Publish( 6606, @event );
			publisher.Publish( 6606, @event, DateTime.Now );

			GoodEvent[] events = new[] { @event };
			publisher.PublishMany( 6606, events );
			publisher.PublishMany( 6606, events, DateTime.Now );
			publisher.ObsoleteAndUnboundedPublishMany( 6606, events );
			publisher.ObsoleteAndUnboundedPublishMany( 6606, events, DateTime.Now );
		}

		public void RefiredEventEnvelopeTests() {

			GoodEvent @event = new GoodEvent();
			RefiredEventEnvelope.Create( 6606, @event );
		}
	}

	public sealed class WithoutEventAttribute {

		public void IEventNotifier( IEventNotifier notifier ) {

			BadEvent @event = new BadEvent();
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ notifier.Publish( @event ) /**/;

			BadEvent[] events = new[] { @event };
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ notifier.PublishMany( events ) /**/;
		}

		public void IEventNotifier( IEventPublisher publisher ) {

			BadEvent @event = new BadEvent();
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.Publish( 6606, @event ) /**/;
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.Publish( 6606, @event, DateTime.Now ) /**/;

			BadEvent[] events = new[] { @event };
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.PublishMany( 6606, events ) /**/;
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.PublishMany( 6606, events, DateTime.Now ) /**/;
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.ObsoleteAndUnboundedPublishMany( 6606, events ) /**/;
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ publisher.ObsoleteAndUnboundedPublishMany( 6606, events, DateTime.Now ) /**/;
		}

		public void RefiredEventEnvelopeTests() {

			BadEvent @event = new BadEvent();
			/* EventTypeMissingEventAttribute(Tests.BadEvent) */ RefiredEventEnvelope.Create( 6606, @event ) /**/;
		}
	}

	public sealed class GenericEvent {

		public void IEventNotifier<T>( IEventNotifier notifier, T @event ) {

			notifier.Publish( @event );

			T[] events = new[] { @event };
			notifier.PublishMany( events );
		}

		public void IEventNotifier<T>( IEventPublisher publisher, T @event ) {

			publisher.Publish( 6606, @event );
			publisher.Publish( 6606, @event, DateTime.Now );

			T[] events = new[] { @event };
			publisher.PublishMany( 6606, events );
			publisher.PublishMany( 6606, events, DateTime.Now );
			publisher.ObsoleteAndUnboundedPublishMany( 6606, events );
			publisher.ObsoleteAndUnboundedPublishMany( 6606, events, DateTime.Now );
		}
	}
}
