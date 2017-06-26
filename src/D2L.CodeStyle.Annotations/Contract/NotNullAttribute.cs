using System;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	/// <seealso cref="NotNullWhenParameterAttribute"/>
	/// <seealso cref="AllowNullAttribute"/>
	/// <seealso cref="AlwaysAssignedValueAttribute"/>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.Property )]
	public sealed class NotNullAttribute : Attribute {
	}

	/// <summary>
	/// All paremters of the given type will implicitly have <see cref="NotNullAttribute"/> applied.
	/// In other words, if a parameter is of a type with this attribute, that parameter cannot be
	/// passed an explicitly null value.
	/// </summary>
	/// <seealso cref="NotNullAttribute"/>
	/// <seealso cref="AllowNullAttribute"/>
	/// <seealso cref="AlwaysAssignedValueAttribute"/>
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate )]
	public sealed class NotNullWhenParameterAttribute : Attribute { }

	/// <summary>
	/// Overrides <see cref="NotNullWhenParameterAttribute"/> for a single parameter, indicating that
	/// the parameter will be allowed to receive a null value.
	/// </summary>
	/// <seealso cref="NotNullAttribute"/>
	/// <seealso cref="NotNullWhenParameterAttribute"/>
	/// <seealso cref="AlwaysAssignedValueAttribute"/>
	[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.Property )]
	public sealed class AllowNullAttribute : Attribute {

		public AllowNullAttribute(
			string rationale
		) {
			Rationale = rationale;
		}

		public string Rationale { get; private set; }

	}

	/// <summary>
	/// Hint to the analyzer to indicate that the specified variable will always be assigned a value.
	/// This should only be used when the NotNull analyzer falsely asserts that a passed argument will
	/// have a null value, but you are certain that it will be assigned a value.
	/// If it is only ever assigned `null`, or is never actually assigned, it will still be flagged.
	/// </summary>
	/// <seealso cref="NotNullAttribute"/>
	/// <seealso cref="NotNullWhenParameterAttribute"/>
	/// <seealso cref="AllowNullAttribute"/>
	[AttributeUsage( AttributeTargets.Constructor | AttributeTargets.Method )]
	public sealed class AlwaysAssignedValueAttribute : Attribute {

		public AlwaysAssignedValueAttribute(
			string variableName,
			string rationale
		) {
			VariableName = variableName;
			Rationale = rationale;
		}

		public string VariableName { get; private set; }
		public string Rationale { get; private set; }

	}
}
