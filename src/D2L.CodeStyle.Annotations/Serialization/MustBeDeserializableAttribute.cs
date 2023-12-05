using System;

namespace D2L.CodeStyle.Annotations.Serialization {
	/// <summary>
	/// Represents a parameter in the call chain of serialization in order to enforce the types used can be deserialized.
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = true )]
	public sealed class MustBeDeserializableAttribute : Attribute {
	}
}
