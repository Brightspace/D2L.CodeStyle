// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// From: https://github.com/dotnet/runtime/commit/6072e4d3a7a2a1493f514cdf4be75a3d56580e84#diff-afc57cc6e9c659ebf491bc78d06cdb5b13d5a049122d8a41f744a515f493663b

using System.ComponentModel;

namespace System.Runtime.CompilerServices {
	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata.
	/// This class should not be used by developers in source code.
	/// </summary>
	[EditorBrowsable( EditorBrowsableState.Never )]
	internal static class IsExternalInit {
	}
}
