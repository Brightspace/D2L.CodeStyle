using System;

namespace D2L.CodeStyle.Annotations.Contract;

/// <summary>
/// Indicates that a string must be a constant value matching the given regex pattern.
/// May be specified multiple times to require the value to satisfy every pattern.
/// </summary>
[AttributeUsage(
	AttributeTargets.Property
		| AttributeTargets.Field
		| AttributeTargets.Parameter,
	AllowMultiple = true
)]
public sealed class PatternStringAttribute : Attribute {

	public PatternStringAttribute( string regexPattern, bool expectMatch = true ) {
		RegexPattern = regexPattern;
		ExpectMatch = expectMatch;
	}

	public string RegexPattern { get; }
	public bool ExpectMatch { get; }

}
