using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.GenericTypeArguments {

	internal static class GenericTypeArgumentsBlacklist {

		public static readonly IReadOnlyDictionary<string, ImmutableArray<ImmutableArray<string>>> BlacklistedTypes = ImmutableDictionary
			.Create<string, ImmutableArray<ImmutableArray<string>>>()
			.Add(
				"D2L.LP.Distributed.Events.Handlers.IEventHandler`1",
				ImmutableArray.Create(
					ImmutableArray.Create( "D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent" )
				)
			)
			.Add(
				"D2L.LP.Distributed.Events.Handlers.IOrgEventHandler`1",
				ImmutableArray.Create(
					ImmutableArray.Create( "D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent" )
				)
			);
	}
}
