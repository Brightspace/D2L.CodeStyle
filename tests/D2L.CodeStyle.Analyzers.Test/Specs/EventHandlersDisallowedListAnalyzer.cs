// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventHandlersDisallowedListAnalyzer

namespace D2L.LP.Distributed.Events.ExternalPublish.UserInteraction {
	public sealed class UserInteractionEvent { }
}

namespace D2L.LP.Distributed.Events.Handlers {
	public interface IEventHandler<T> { }
	public interface IOrgEventHandler<T> { }
}

namespace UnrelatedImplementation {

	using System.Collections.Generic;

	public sealed class IntComparer : IComparer<int> {
		int IComparer<int>.Compare( int x, int y ) {
			return x.CompareTo( y );
		}
	}
}

namespace AllowedImplementations {

	using D2L.LP.Distributed.Events.Handlers;

	public sealed class StringEventHandler : IEventHandler<string> { }
	public sealed class StringOrgEventHandler : IOrgEventHandler<string> { }
}

namespace DisallowedImplementations {

	using D2L.LP.Distributed.Events.ExternalPublish.UserInteraction;
	using D2L.LP.Distributed.Events.Handlers;

	public sealed class /* EventHandlerDisallowed(D2L.LP.Distributed.Events.Handlers.IEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ UserInteractionEventHandler /**/
		: IEventHandler<UserInteractionEvent> { }

	public sealed class /* EventHandlerDisallowed(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ UserInteractionOrgEventHandler /**/
		: IOrgEventHandler<UserInteractionEvent> { }

	public sealed class /* EventHandlerDisallowed(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ MultiEventHandler_UserInteractionOrgEventHandler_First /**/
		: IOrgEventHandler<UserInteractionEvent>, IOrgEventHandler<string> { }

	public sealed class /* EventHandlerDisallowed(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ MultiEventHandler_UserInteractionOrgEventHandler_Last /**/
		: IOrgEventHandler<string>, IOrgEventHandler<UserInteractionEvent>{ }

}
