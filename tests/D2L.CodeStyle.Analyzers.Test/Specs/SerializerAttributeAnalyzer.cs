// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.SerializerAttributeAnalyzer

using System;
using System.ComponentModel;
using D2L.LP.Serialization;

namespace D2L.LP.Serialization {
	public sealed class SerializerAttribute : Attribute {
		public SerializableAttribute( Type type ) { }
	}
	public interface ITrySerializer { }
}

namespace SpecTests {

	[Serializer( typeof( Nested.Serializer ) )]
	public sealed class Nested {
		private sealed class Serializer : ITrySerializer { }
	}

	[Serializer( typeof( ExternalSerializer ) )]
	public sealed class External { }
	public sealed class ExternalSerializer : ITrySerializer { }

	[Serializer( /* InvalidSerializerType(System.String) */ typeof( string ) /**/ )]
	public sealed class InvalidSerializerType { }

	[Serializer( /* InvalidSerializerType(null) */ null /**/ )]
	public sealed class NullSerializerType { }

	[Serializer( /* InvalidSerializerType(123) */ 123 /**/ )]
	public sealed class InvalidSerializerTypeArgument { }

	[Serializer]
	public sealed class IncompleteAttribute_WithoutParentheses { }

	[Serializer()]
	public sealed class IncompleteAttribute_WithParentheses { }

	[Category( "Not related" )]
	public sealed class RandomAttribute { }
}
