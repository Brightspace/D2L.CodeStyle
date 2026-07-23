using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a value must be a compile-time constant. May be applied to a
	/// method/indexer parameter, a property, a field or a method return value.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Parameter
			| AttributeTargets.Property
			| AttributeTargets.Field
			| AttributeTargets.ReturnValue,
		AllowMultiple = false
	)]
	public sealed class ConstantAttribute : ReadOnlyAttribute {
	}
}
