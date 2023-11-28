// analyzer: D2L.CodeStyle.Analyzers.Pinning.RecursivelyDeserializableAnalyzer

using System;
using System.Collections.Generic;
using D2L.CodeStyle.Annotations.Pinning;

using D2L.LP.Serialization;

namespace D2L.LP.Serialization {

	[AttributeUsage(
		AttributeTargets.Class,
		AllowMultiple = false,
		Inherited = false
	)]
	public sealed class ReflectionSerializerAttribute : Attribute {
	}

	[AttributeUsage(
			AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,
			AllowMultiple = false,
			Inherited = false
	)]
	public sealed class SerializerAttribute : Attribute {

		private readonly Type m_type;

		public SerializerAttribute( Type type ) {

			if( type == null ) {
				throw new ArgumentNullException( nameof( type ) );
			}

			m_type = type;
		}

		public Type Type {
			get { return m_type; }
		}
	}
}

namespace D2L.Pinning.Recursive.Test {

	[ReflectionSerializer]
	public class EmptyPinnedNotRecursively {}

	[ReflectionSerializer]
	public class EmptyPinnedRecursively {}

	[ReflectionSerializer]
	public class EmptyGenericPinnedRecursively<T> {}

	[ReflectionSerializer]
	public class PinnedRecursivelyWithSafeTypes {
		public int ThisIsFine { get; }
		public string ThisIsAFineString { get; }
	}

	[ReflectionSerializer]
	public class PinnedRecursivelyWithUnsafeTypes {
		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public object ThisIsNotFine { get; set; }/**/
		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public Dictionary<string, object>  ThisIsNotAFineDictionary { get; set; }/**/
	}

	[ReflectionSerializer]
	public class PinnedRecursivelyWithBasicTypesInAllowedTypes {
		public Dictionary<string, string> ThisIsAFineDictionary { get; }
	}

	// The following go hand-in-hand with the MustBeDeserializable analysis to ensure the sets are safe
	[ReflectionSerializer]
	public sealed record UnsafeRecord<T>(
		/* ReflectionSerializerDescendantsMustBeDeserializable() */ T Value /**/,
		/* ReflectionSerializerDescendantsMustBeDeserializable() */ object O /**/);

	[ReflectionSerializer]
	public sealed record SafeRecord<[MustBeDeserializable] T>( T Value );

	[ReflectionSerializer]
	public class GenericPinnedRecursivelyWithoutMustBeDeserializable<T> {
		public GenericPinnedRecursivelyWithoutMustBeDeserializable(
			T value ) {
			Unsafe = value;
		}
		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public T Unsafe { get; set; } /**/

		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public object UnsafeObject => Unsafe; /**/

		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public Type UnsafeType { get { return typeof(T); } } /**/
	}

	[ReflectionSerializer]
	public class GenericPinnedRecursivelyWithMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericPinnedRecursivelyWithMustBeDeserializableConstructor(
			[MustBeDeserializable] T value ) {
			Unsafe = value;
		}

		public T Unsafe { get; }

		public object UnsafeObject => Unsafe;

		public Type UnsafeType { get { return typeof(T); } }
	}

	[ReflectionSerializer]
	public class NonGenericPinnedRecursivelyWithMustBeDeserializableConstructor {
		private object m_unsafeField;
		public GenericPinnedRecursivelyWithMustBeDeserializableConstructor(
			[MustBeDeserializable] object value ) {
			SafeObject = value;
			UnsafeObject = value;
			m_unsafeField = value;
		}

		public readonly object SafeObject { get; }

		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public object UnsafeObject { get; set; } /**/

		/* ReflectionSerializerDescendantsMustBeDeserializable() */ public object UnsafeObject2 => m_unsafeField; /**/
	}
}
