// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Events.EventHandlersBlacklistAnalyzer

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

namespace NonBlacklistedImplementations {

	using D2L.LP.Distributed.Events.Handlers;

	public sealed class StringEventHandler : IEventHandler<string> { }
	public sealed class StringOrgEventHandler : IOrgEventHandler<string> { }
}

namespace BlacklistedImplementations {

	using D2L.LP.Distributed.Events.ExternalPublish.UserInteraction;
	using D2L.LP.Distributed.Events.Handlers;

	public sealed class UserInteractionEventHandler :/* EventHandlerBlacklisted(D2L.LP.Distributed.Events.Handlers.IEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ IEventHandler<UserInteractionEvent> /**/{ }
	public sealed class UserInteractionOrgEventHandler :/* EventHandlerBlacklisted(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ IOrgEventHandler<UserInteractionEvent> /**/{ }

	public sealed class UserInteractionOrgEventHandler : IOrgEventHandler<string>,/* EventHandlerBlacklisted(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ IOrgEventHandler<UserInteractionEvent> /**/{ }
	public sealed class UserInteractionOrgEventHandler :/* EventHandlerBlacklisted(D2L.LP.Distributed.Events.Handlers.IOrgEventHandler<D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent>) */ IOrgEventHandler<UserInteractionEvent> /**/, IOrgEventHandler<string> { }

}