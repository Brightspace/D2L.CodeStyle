﻿#if !NETSTANDARD2_1

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage( AttributeTargets.Parameter )]
internal sealed class NotNullWhenAttribute : Attribute {
	public bool ReturnValue { get; }

	public NotNullWhenAttribute( bool returnValue ) {
		ReturnValue = returnValue;
	}
}

#endif
