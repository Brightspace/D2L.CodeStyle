using System;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToTypeAttribute : Attribute {

		/// <summary>
		/// Restricts the visibility to the specified type.
		/// </summary>
		/// <param name="type">The non-generic type or the generic type definition (e.g. System.Span&lt;&gt;) to be made visible to.</param>
		/// <param name="inherited">Whether the restriction should also apply to derived types.</param>
		public OnlyVisibleToTypeAttribute(
			Type type,
			bool inherited = true
		)
			: this( type.FullName, type.Assembly.GetName().Name, inherited ) {
		}

		/// <summary>
		/// Restricts the visibility to the specified type.
		/// </summary>
		/// <param name="fullyQualifiedTypeName">The non-generic type or the generic type definition (e.g. System.Span`1) to be made visible to.</param>
		/// <param name="assemblyName">The name of the assembly containing the type.</param>
		/// <param name="inherited">Whether the restriction should also apply to derived types.</param>
		public OnlyVisibleToTypeAttribute(
			string fullyQualifiedTypeName,
			string assemblyName,
			bool inherited = true
		) {
			FullyQualifiedTypeName = fullyQualifiedTypeName;
			AssemblyName = assemblyName;
			Inherited = inherited;
		}

		public string FullyQualifiedTypeName { get; }
		public string AssemblyName { get; }
		public bool Inherited { get; }
	}
}
