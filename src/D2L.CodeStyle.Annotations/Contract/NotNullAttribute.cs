using System;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.Property )]
	public sealed class NotNullAttribute : Attribute {
	}

	/// <summary>
	/// All paremters of the given type will implicitly have <see cref="NotNullAttribute"/> applied.
	/// In other words, if a parameter is of a type with this attribute, that parameter cannot be
	/// passed an explicitly null value.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate )]
	public sealed class NotNullWhenParameterAttribute : Attribute { }

	/// <summary>
	/// Overrides <see cref="NotNullWhenParameterAttribute"/> for a single parameter, indicating that
	/// the parameter will be allowed to receive a null value.
	/// </summary>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.Property )]
	public sealed class AllowNullAttribute : Attribute {}
}
