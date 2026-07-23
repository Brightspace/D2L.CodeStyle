using System;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a string must be a constant value matching the given regex
	/// pattern. May be applied to a property or a field.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Property
			| AttributeTargets.Field,
		AllowMultiple = false
	)]
	public sealed class PatternStringAttribute : ReadOnlyAttribute {

		public PatternStringAttribute( string regexPattern, bool expectMatch = true ) {
			RegexPattern = regexPattern;
			ExpectMatch = expectMatch;
		}

		public string RegexPattern { get; }
		public bool ExpectMatch { get; }
	}
}
