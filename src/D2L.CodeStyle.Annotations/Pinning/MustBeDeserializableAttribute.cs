using System;

namespace D2L.CodeStyle.Annotations.Pinning {
	/// <summary>
	/// Represents a parameter in the call chain of serialization in order to enforce pinning or otherwise set limitations on serialized types
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false )]
	public sealed class MustBeDeserializableAttribute : Attribute {
	}
}
