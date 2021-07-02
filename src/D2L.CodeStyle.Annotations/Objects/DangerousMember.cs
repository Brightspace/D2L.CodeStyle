using System;

namespace D2L.CodeStyle.Annotations {
	public static partial class Objects {

		[AttributeUsage( validOn: AttributeTargets.Method )]
		public sealed class DangerousMember : Attribute { }
	}
}
