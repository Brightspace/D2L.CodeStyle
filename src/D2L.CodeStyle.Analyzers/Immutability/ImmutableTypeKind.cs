#nullable disable

using System;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// Our "immutability" is a property of values. However, we can also
	/// generalize this to types as a whole to varying degrees.
	/// </summary>
	[Flags]
	internal enum ImmutableTypeKind {
		/// <summary>
		/// We have nothing to say about the immutability of this type.
		/// </summary>
		None = 0,

		/// <summary>
		/// A type T has immutable instances if "new T( ... )" always produces
		/// an immutable value.
		/// </summary>
		Instance = 1,


		/// <summary>
		/// A type T is totally immutable if all objects of type T are
		/// immutable. This is a stronger condition than instance immutability.
		/// </summary>
		Total = 2 | Instance,
	}
}
