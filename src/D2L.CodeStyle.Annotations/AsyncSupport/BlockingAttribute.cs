using System;

namespace D2L.CodeStyle.Annotations {
	/// <summary>
	/// Indicates that this method may block on I/O. It must not be called from async methods.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Method,
		AllowMultiple = false,
		// We require something like explicit inheritance because
		// (1) its clearer to have it explicit.
		// (2) implicit interface impls don't inherit attributes.
		Inherited = false
	)]
	public sealed class BlockingAttribute : Attribute {}
}
