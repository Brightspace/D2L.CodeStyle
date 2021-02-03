using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a parameter must be called with a constant value
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public sealed class ConstantAttribute : ReadOnlyAttribute {
	}
}
