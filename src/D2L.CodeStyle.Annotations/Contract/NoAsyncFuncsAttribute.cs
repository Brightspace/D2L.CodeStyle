using System;

namespace D2L.CodeStyle.Annotations.Contract;

[AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false )]
public sealed class NoAsyncFuncsAttribute : Attribute {

	public string Message { get; set; }

}
