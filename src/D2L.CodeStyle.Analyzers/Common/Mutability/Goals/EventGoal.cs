using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct EventGoal : Goal {
		public EventGoal( IEventSymbol ev ) {
			Event = ev;
		}

		public IEventSymbol Event { get; }
	}
}
