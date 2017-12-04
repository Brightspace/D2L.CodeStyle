using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Statics {
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
