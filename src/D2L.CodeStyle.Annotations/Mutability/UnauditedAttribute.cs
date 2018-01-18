using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Mutability {
		[AttributeUsage( validOn: AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event )]
		public sealed class UnauditedAttribute : Attribute {
			public readonly Because m_cuz;
			public readonly UndiffBucket m_bucket;

			public UnauditedAttribute( Because why ) {
				if( why == Because.None ) {
					throw new ArgumentException( "None is not a valid Unaudited reason", nameof( why ) );
				}
				if( Math.Abs( Math.Log( (int)why, 2 ) % 1 ) <= double.Epsilon ) {
					throw new ArgumentException( "Because can not be multiple values for an Unaudited reason", nameof( why ) );
				}
				m_cuz = why;
			}

			public UnauditedAttribute( Because why, UndiffBucket bucket )
				: this( why )
			{
				if( why != Because.ItsStickyDataOhNooo ) {
					throw new ArgumentException( "UndiffBucket is only meaningful for Because.ItsStickyDataOhNooo", nameof( bucket ) );
				}
				m_bucket = bucket;
			}
		}
	}
}
