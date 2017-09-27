using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	/// <seealso cref="NotNullWhenParameterAttribute"/>
	/// <seealso cref="AllowNullAttribute"/>
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public sealed class NotNullAttribute : Attribute {
	}

	/// <summary>
	/// All paremters of the given type will implicitly have <see cref="NotNullAttribute"/> applied.
	/// In other words, if a parameter is of a type with this attribute, that parameter cannot be
	/// passed an explicitly null value.
	/// </summary>
	/// <seealso cref="NotNullAttribute"/>
	/// <seealso cref="AllowNullAttribute"/>
	[AttributeUsage(
		AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = false
	)]
	public sealed class NotNullWhenParameterAttribute : Attribute { }

	/// <summary>
	/// Overrides <see cref="NotNullWhenParameterAttribute"/> for a single parameter, indicating that
	/// the parameter will be allowed to receive a null value.
	/// </summary>
	/// <seealso cref="NotNullAttribute"/>
	/// <seealso cref="NotNullWhenParameterAttribute"/>
	[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
	public sealed class AllowNullAttribute : Attribute {

		public AllowNullAttribute(
			string rationale
		) {
			Rationale = rationale;
		}

		public string Rationale { get; private set; }
	}
}
