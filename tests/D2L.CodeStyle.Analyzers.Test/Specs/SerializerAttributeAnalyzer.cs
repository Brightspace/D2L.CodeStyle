﻿// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.SerializerAttributeAnalyzer, D2L.CodeStyle.Analyzers

using System;
using D2L.LP.Serialization;

namespace D2L.LP.Serialization {
	public sealed class SerializerAttribute : Attribute {
		public SerializableAttribute( Type type ) { }
	}
	public interface ISerializer { }
	public interface ITrySerializer { }
}

namespace SpecTests {

	[Serializer( typeof( Nested.TrySerializer ) )]
	public sealed class Nested {
		private sealed class TrySerializer : ITrySerializer { }
	}

	[Serializer( typeof( NestedGeneric<>.TrySerializer ) )]
	public sealed class NestedGeneric<T> {
		private sealed class TrySerializer : ITrySerializer { }
	}

	[Serializer( typeof( ExternalSerializer ) )]
	public sealed class External { }
	public sealed class ExternalSerializer : ISerializer { }

	[Serializer( /* InvalidSerializerType(System.String) */ typeof( string ) /**/ )]
	public sealed class InvalidSerializerType { }

	[Serializer( /* InvalidSerializerType(SpecTests.InvalidGenericSerializerType.Comparer) */ typeof( InvalidGenericSerializerType<>.Comparer ) /**/ )]
	public sealed class InvalidGenericSerializerType<T> {
		private sealed class Comparer { }
	}

	[Serializer( /* InvalidSerializerType(null) */ null /**/ )]
	public sealed class NullSerializerType { }

	// This is simply a compilation failure as there is no conversion from int -> Type
	[Serializer( 123 )]
	public sealed class InvalidSerializerTypeArgument { }

	[Serializer]
	public sealed class IncompleteAttribute_WithoutParentheses { }

	[Serializer()]
	public sealed class IncompleteAttribute_WithParentheses { }

	[System.ComponentModel.Category( "Not related" )]
	public sealed class RandomAttribute { }

	[Invalid]
	public sealed class InvalidAttribute { }

	[]
	public sealed class JustSomeBrackets { }
}
