using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public enum Because {
		ItHasntBeenLookedAt,
		ItsSketchy,
		ItsStickyDataOhNooo,
		WeNeedToMakeTheAnalyzerConsiderThisSafe
	}

	public static partial class Statics {
		[Obsolete( "Static variables marked as unaudited require auditing. Only use this attribute as a temporary measure in assemblies." )]
		[AttributeUsage( validOn: AttributeTargets.Field | AttributeTargets.Property )]
		public sealed class Unaudited : Attribute {
			public readonly Because m_cuz;

			public Unaudited( Because why ) {
				m_cuz = why;
			}
		}
	}
}
