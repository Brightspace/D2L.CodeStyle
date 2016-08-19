using System;

// ReSharper disable once CheckNamespace
namespace D2L {
	public static partial class CodeStyle {
		public static partial class Statics {
			[Obsolete( "Static variables marked as unaudited require auditing. Only use this attribute as a temporary measure in assemblies." )]
			[AttributeUsage( validOn: AttributeTargets.Field, Inherited = false )]
			public sealed class Unaudited : Attribute {
			}
		}
	}
}