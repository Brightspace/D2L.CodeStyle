using System;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		AttributeTargets.Constructor | AttributeTargets.Method,
		AllowMultiple = false
	)]
	public sealed class RequireNamedArgumentsAttribute : Attribute {
	}
}
