// analyzer: D2L.CodeStyle.Analyzers.Pinning.PinnedAttributeAnalyzer


using D2L.CodeStyle.Annotations.Pinning;

namespace D2L.Pinning.Test {
	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public class PinnedProperly {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectName /**/ {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public interface /* PinnedTypesMustNotMove() */ IPinnedWithIncorrectName /**/ {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public record /* PinnedTypesMustNotMove() */ RecordPinnedWithIncorrectName /**/ {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public struct /* PinnedTypesMustNotMove() */ StructPinnedWithIncorrectName /**/ {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.PinnedWithIncorrectAssembly", assembly: "WrongPinnedAttributeAnalyzer", pinnedRecursively: false)]
	public class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectAssembly /**/ {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.GenericPinnedProperly<T>", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public class GenericPinnedProperly<T> {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.Test.GenericPinnedWithIncorrectName<TWrong>", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
	public class /* PinnedTypesMustNotMove() */ GenericPinnedWithIncorrectName/**/<T>  {}

	// test that classes pinned inside an unpinned class are handled reasonably
	public class Outer {
		[Pinned(fullyQualifiedName: "D2L.Pinning.Test.Outer.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
		private class PinnedProperly {}

		[Pinned(fullyQualifiedName: "D2L.Pinning.Outer.PinnedProperly", assembly: "PinnedAttributeAnalyzer", pinnedRecursively: false)]
		private class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectName /**/ {}
	}
}
