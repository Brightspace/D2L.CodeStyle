using System;

namespace D2L.CodeStyle.Annotations.Attributes {
	/// <summary>
	/// An attribute to mark that an instance of this type may be reused for multiple requests
	/// </summary>
	[AttributeUsage( AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false )]
	public sealed class CustomerStateAttribute : Attribute {
	}
}
