using System;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToTypeAttribute : Attribute {

		/// <summary>
		/// Restricts the visibility to the specified type.
		/// </summary>
		/// <param name="type">The non-generic type or the generic type definition (e.g. System.Span&lt;&gt;) to be made visible to.</param>
		public OnlyVisibleToTypeAttribute( Type type )
			: this( type.FullName, type.Assembly.GetName().Name ) {
		}

		/// <summary>
		/// Restricts the visibility to the specified type.
		/// </summary>
		/// <param name="fullyQualifiedTypeName">The non-generic type or the generic type definition (e.g. System.Span`1) to be made visible to.</param>
		/// <param name="assemblyName">The name of the assembly containing the type.</param>
		public OnlyVisibleToTypeAttribute( string fullyQualifiedTypeName, string assemblyName ) {
			FullyQualifiedTypeName = fullyQualifiedTypeName;
			AssemblyName = assemblyName;
		}

		public string FullyQualifiedTypeName { get; }
		public string AssemblyName { get; }
	}
}
