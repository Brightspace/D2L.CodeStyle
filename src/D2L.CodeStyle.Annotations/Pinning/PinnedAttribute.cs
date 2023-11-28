using System;

namespace D2L.CodeStyle.Annotations.Pinning {
	/// <summary>
	/// The type should not be moved from its assembly due to risk of deserialization based on assembly qualified name or other dangerous call chains
	/// </summary>
	[AttributeUsage( AttributeTargets.Class
		| AttributeTargets.Struct
		| AttributeTargets.Interface
		, AllowMultiple = false, Inherited = false )]
	public sealed class PinnedAttribute : Attribute {
		public string FullyQualifiedName { get; }
		public string Assembly { get; }

		public PinnedAttribute( string fullyQualifiedName, string assembly ) {
			FullyQualifiedName = fullyQualifiedName;
			Assembly = assembly;
		}
	}
}
