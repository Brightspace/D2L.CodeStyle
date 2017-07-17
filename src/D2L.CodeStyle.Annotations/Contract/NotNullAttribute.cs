using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public sealed class NotNullAttribute : Attribute {
	}
}
