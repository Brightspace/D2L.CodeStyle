using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Events {

	internal static class EventHandlersDisallowedList {

		private static readonly ImmutableArray<ImmutableArray<string>> DisallowedEventHandlerTypes = ImmutableArray.Create(

				// External only
				ImmutableArray.Create( "D2L.LP.Distributed.Events.ExternalPublish.UserInteraction.UserInteractionEvent" ),

				// Broadcast events
				ImmutableArray.Create( "D2L.Core.JobManagement.Events.JobAbortRequested" ),
				ImmutableArray.Create( "D2L.LP.Distributed.WorkQueues.Events.WorkQueueEvent" ),
				ImmutableArray.Create( "D2L.LP.Tasks.Events.CancelTaskEvent" ),
				ImmutableArray.Create( "D2L.LP.Tools.DataPurgeArchive.PurgeTasks.Events.AbortPurgeTaskEvent" ),
				ImmutableArray.Create( "D2L.LP.Tools.DataPurgeArchive.PurgeTasks.Events.PausePurgeTaskEvent" ),
				ImmutableArray.Create( "D2L.LP.Tools.DataPurgeArchive.PurgeTasks.Scheduling.Events.OrgScheduledEvent" ),
				ImmutableArray.Create( "D2L.LP.Tools.DataPurgeArchive.PurgeTasks.Scheduling.Events.OrgUnscheduledEvent" ),
				ImmutableArray.Create( "D2L.AP.S3.Analysis.Service.PredictiveModelingAbortEvent" )
			);

		public static readonly IReadOnlyDictionary<string, ImmutableArray<ImmutableArray<string>>> DisallowedTypes = ImmutableDictionary
			.Create<string, ImmutableArray<ImmutableArray<string>>>()
			.Add(
				"D2L.LP.Distributed.Events.Handlers.IEventHandler`1", 
				DisallowedEventHandlerTypes
			)
			.Add(
				"D2L.LP.Distributed.Events.Handlers.IOrgEventHandler`1", 
				DisallowedEventHandlerTypes
			);
	}
}
