using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
    public static partial class Statics {
		/// <summary>
		/// Define a list of known immutable types used in the assembly.
		/// </summary>
		[AttributeUsage( validOn: AttributeTargets.Assembly, AllowMultiple = false )]
        public sealed class KnownImmutableTypes : Attribute {
			/// <summary>
			/// Define a list of known immutable types used in the assembly.
			/// </summary>
			/// <param name="typeNames">Type names, fully qualified, but not assembly qualified.</param>
			public KnownImmutableTypes( params string[] typeNames ) {
				TypeNames = typeNames;
			}

            public string[] TypeNames { get; }
        }
	}
}
