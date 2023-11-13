using System;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class ReleaseVisibilityConstraintsAttribute : Attribute {

		/// <summary>
		/// Removes any restrictions to the visibility of the object
		/// which were imposed on the specified base type.
		/// </summary>
		/// <param name="type">The non-generic type or the generic type definition (e.g. System.Span&lt;&gt;) of the base which holds constraints to visibility.</param>
		public ReleaseVisibilityConstraintsAttribute( Type type )
			: this( type.FullName, type.Assembly.GetName().Name ) {
		}

		/// <summary>
		/// Removes any restrictions to the visibility of the object
		/// which were imposed on the specified base type.
		/// </summary>
		/// <param name="fullyQualifiedTypeName">The non-generic type or the generic type definition (e.g. System.Span`1) of the base which holds constraints to visibility.</param>
		/// <param name="assemblyName">The name of the assembly containing the base type.</param>
		public ReleaseVisibilityConstraintsAttribute( string fullyQualifiedTypeName, string assemblyName ) {
			FullyQualifiedTypeName = fullyQualifiedTypeName;
			AssemblyName = assemblyName;
		}

		public string FullyQualifiedTypeName { get; }
		public string AssemblyName { get; }
	}
}
