using System;

namespace D2L.CodeStyle.Annotations {
	public static partial class Objects {

		[AttributeUsage( validOn: AttributeTargets.Property )]
		public sealed class DangerousProperty : Attribute { }
	}
}
