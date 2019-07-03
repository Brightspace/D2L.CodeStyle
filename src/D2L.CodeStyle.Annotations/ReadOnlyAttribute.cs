using System;

namespace D2L.CodeStyle.Annotations {
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public class ReadOnlyAttribute : Attribute {}
}
