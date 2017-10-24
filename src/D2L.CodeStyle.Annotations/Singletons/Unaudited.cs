using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Singletons {
		[Obsolete( "Singletons marked as unaudited require auditing. Only use this attribute as a temporary measure in assemblies." )]
		[AttributeUsage( validOn: AttributeTargets.Class )]
		public sealed class UnauditedAttribute : Attribute {
			public readonly Because m_cuz;
			public readonly UndiffBucket m_bucket;

			public UnauditedAttribute( Because why ) {
				m_cuz = why;
			}

			public UnauditedAttribute( Because why, UndiffBucket bucket ) {
				if ( why != Because.ItsStickyDataOhNooo ) {
					throw new ArgumentException( "UndiffBucket is only meaningful for Because.ItsStickyDataOhNooo", nameof( bucket ) );
				}
				m_cuz = why;
				m_bucket = bucket;
			}
		}
	}
}
