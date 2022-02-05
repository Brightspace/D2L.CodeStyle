using System;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToAttribute : Attribute {

		public OnlyVisibleToAttribute( string fullyQualifiedMetadataName ) {
			FullyQualifiedMetadataName = fullyQualifiedMetadataName;
		}

		public string FullyQualifiedMetadataName { get; }
	}
}
