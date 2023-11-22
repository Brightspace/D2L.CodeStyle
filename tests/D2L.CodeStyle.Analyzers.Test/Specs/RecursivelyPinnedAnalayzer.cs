// analyzer: D2L.CodeStyle.Analyzers.Pinning.RecursivelyPinnedAnalyzer

using System.Collections.Generic;
using D2L.CodeStyle.Annotations.Pinning;

namespace D2L.Pinning.Recursive.Test {

	[Pinned(fullyQualifiedName: "D2L.Pinning.Recursive.Test.EmptyPinnedNotRecursively", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: false)]
	public class EmptyPinnedNotRecursively {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Recursive.Test.EmptyPinnedRecursively", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true)]
	public class EmptyPinnedRecursively {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Recursive.Test.EmptyGenericPinnedRecursively<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true)]
	public class EmptyGenericPinnedRecursively<T> {}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.PinnedRecursivelyWithBasicTypes", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class PinnedRecursivelyWithSafeTypes {
		public int ThisIsFine { get; }
		public string ThisIsAFineString { get; }
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.PinnedRecursivelyWithUnsafeTypes", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class PinnedRecursivelyWithUnsafeTypes {
		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public object ThisIsNotFine { get; set; }/**/
		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public Dictionary<string, object>  ThisIsNotAFineDictionary { get; set; }/**/
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.PinnedRecursivelyWithBasicTypesInAllowedTypes", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class PinnedRecursivelyWithBasicTypesInAllowedTypes {
		public Dictionary<string, string> ThisIsAFineDictionary { get; }
	}

	// The following go hand-in-hand with the MustBeDeserializable analysis to ensure the sets are safe
	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.NotDeserializableObjectRecord<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public sealed record UnsafeRecord<T>(
		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ T Value /**/,
		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ object O /**/);

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.SafeRecord<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public sealed record SafeRecord<[MustBeDeserializable] T>( T Value );

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.GenericPinnedRecursivelyWithoutMustBeDeserializable<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class GenericPinnedRecursivelyWithoutMustBeDeserializable<T> {
		public GenericPinnedRecursivelyWithoutMustBeDeserializable(
			T value ) {
			Unsafe = value;
		}
		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public T Unsafe { get; set; } /**/

		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public object UnsafeObject => Unsafe; /**/

		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public Type UnsafeType { get { return typeof(T); } } /**/
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.GenericPinnedRecursivelyWithoutMustBePinnedConstructor<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class GenericPinnedRecursivelyWithMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericPinnedRecursivelyWithMustBeDeserializableConstructor(
			[MustBeDeserializable] T value ) {
			Unsafe = value;
		}

		public T Unsafe { get; }

		public object UnsafeObject => Unsafe;

		public Type UnsafeType { get { return typeof(T); } }
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.NonGenericPinnedRecursivelyWithMustBeDeserializableConstructor", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true )]
	public class NonGenericPinnedRecursivelyWithMustBeDeserializableConstructor {
		private object m_unsafeField;
		public GenericPinnedRecursivelyWithMustBeDeserializableConstructor(
			[MustBeDeserializable] object value ) {
			SafeObject = value;
			UnsafeObject = value;
			m_unsafeField = value;
		}

		public readonly object SafeObject { get; }

		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public object UnsafeObject { get; set; } /**/

		/* RecursivePinnedDescendantsMustBeRecursivelyPinned() */ public object UnsafeObject2 => m_unsafeField; /**/
	}
}
