using System;

namespace D2L.CodeStyle.Annotations {
	/// <summary>
	/// Indicates that a blocking/synchronous version of the method should be
	/// generated.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Method,
		AllowMultiple = false,
		Inherited = false
	)]
	public sealed class GenerateSyncAttribute : Attribute {}
}
