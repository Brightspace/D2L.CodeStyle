using System;

namespace D2L.CodeStyle.Annotations.Pinning {
	/// <summary>
	/// Represents a parameter in a dangerous call chain (e.g. serialization using the assembly qualified name) that must be pinned
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false )]
	public sealed class MustBePinnedAttribute : Attribute {
	}
}
