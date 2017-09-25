using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public enum Because {
		/// <summary>
		/// DEPRECATED: Things marked with this attribute should be recategorized.
		/// </summary>
		ItHasntBeenLookedAt,

		/// <summary>
		/// DEPRECATED: Things marked with this attribute should be recategorized.
		/// This was used for things that are ugly code, possibly ugly enough to
		/// be an undiff problem. We need to better categorize these so that we can
		/// prioritize work that gets us closer to undiff.
		/// </summary>
		ItsSketchy,

		/// <summary>
		/// These are blockers for undiff.
		/// </summary>
		ItsStickyDataOhNooo,

		/// <summary>
		/// These are things which could be analyzed as safe but require improvements
		/// to the analyzer. An alternative is [Statics.Audited(...)] when we don't
		/// expect an improvements to the analyzer to pass this variable.
		/// </summary>
		WeNeedToMakeTheAnalyzerConsiderThisSafe,

		/// <summary>
		/// This code is ugly and we don't feel comfortable making it
		/// [Statics.Audited(...)] (which is a long-term acceptable bucket) we
		/// are not prioritizing this work for undiff.
		/// </summary>
		ItsUgly,

		/// <summary>
		/// This code won't ship or be enabled for undiff clients. It
		/// constitutes a feature that is not relevant for them, possibly
		/// because there is a newer parallel implementation or it is not
		/// compatible with next-gen hosting.
		/// </summary>
		ItsOnDeathRow
	}

	public enum UndiffBucket {
		/// <summary>
		/// This variable is required for all undiff clients
		/// </summary>
		Core,

		WebDAV,
		D2LWS,
		Banner,
		IPSIS
	}

	public static partial class Statics {
		[Obsolete( "Static variables marked as unaudited require auditing. Only use this attribute as a temporary measure in assemblies." )]
		[AttributeUsage( validOn: AttributeTargets.Field | AttributeTargets.Property )]
		public sealed class Unaudited : Attribute {
			public readonly Because m_cuz;
			public readonly UndiffBucket m_bucket;

			public Unaudited( Because why ) {
				m_cuz = why;
			}

			public Unaudited( Because why, UndiffBucket bucket ) {
				if ( why != Because.ItsStickyDataOhNooo ) {
					throw new ArgumentException( "UndiffBucket is only meaningful for Because.ItsStickyDataOhNooo", nameof( bucket ) );
				}
				m_cuz = why;
				m_bucket = bucket;
			}
		}
	}
}
