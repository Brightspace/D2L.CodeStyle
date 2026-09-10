using System;
using System.Diagnostics.CodeAnalysis;

namespace D2L.CodeStyle.Annotations.Contract;

/// <summary>
/// Indicates that a string must be a constant value matching the given regex pattern.
/// May be specified multiple times to require the value to satisfy every pattern.
/// </summary>
[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = true )]
public sealed class PatternStringAttribute : Attribute {

	public PatternStringAttribute(
		[StringSyntax( StringSyntaxAttribute.Regex )] string regexPattern
	) {
		RegexPattern = regexPattern;
	}

	public string RegexPattern { get; }
	public bool ExpectMatch { get; set; } = true;

}
