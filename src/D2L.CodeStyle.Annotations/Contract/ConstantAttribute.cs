using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater must be called with a constant value
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public sealed class ConstantAttribute : Attribute {
	}
}
