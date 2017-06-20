using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public sealed class NotNullAttribute : Attribute {
	}
}
