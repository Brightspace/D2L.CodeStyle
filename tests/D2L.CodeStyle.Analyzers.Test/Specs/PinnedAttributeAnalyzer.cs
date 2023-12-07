// analyzer: D2L.CodeStyle.Analyzers.Pinning.PinnedAttributeAnalyzer


using D2L.CodeStyle.Annotations.Pinning;

namespace D2L.Pinning.Test {
	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
	public class PinnedProperly { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
	public class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectName /**/ { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
	public interface /* PinnedTypesMustNotMove() */ IPinnedWithIncorrectName /**/ { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
	public record /* PinnedTypesMustNotMove() */ RecordPinnedWithIncorrectName /**/ { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
	public struct /* PinnedTypesMustNotMove() */ StructPinnedWithIncorrectName /**/ { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.PinnedWithIncorrectAssembly", assembly: "WrongPinnedAttributeAnalyzer" )]
	public class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectAssembly /**/ { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.GenericPinnedProperly<T>", assembly: "PinnedAttributeAnalyzer" )]
	public class GenericPinnedProperly<T> { }

	[Pinned( fullyQualifiedName: "D2L.Pinning.Test.GenericPinnedWithIncorrectName<TWrong>", assembly: "PinnedAttributeAnalyzer" )]
	public class /* PinnedTypesMustNotMove() */ GenericPinnedWithIncorrectName/**/<T> { }

	// test that classes pinned inside an unpinned class are handled reasonably
	public class Outer {
		[Pinned( fullyQualifiedName: "D2L.Pinning.Test.Outer.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
		private class PinnedProperly { }

		[Pinned( fullyQualifiedName: "D2L.Pinning.Outer.PinnedProperly", assembly: "PinnedAttributeAnalyzer" )]
		private class /* PinnedTypesMustNotMove() */ PinnedWithIncorrectName /**/ { }
	}
}
