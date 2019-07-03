// analyzer: D2L.CodeStyle.Analyzers.Language.StructShouldBeReadonlyAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs.Good {

	public readonly struct EmptyStruct { }

	public readonly struct StructWithFieldsAndMembers {

		public const int ConstField = 0;
		public readonly int ReadonlyField = 0;
		public int ReadonlyProperty { get; } = 0;

	}

	public readonly partial struct PartialStructWithFieldsAndMembers {

		public const int ConstField = 0;
		public readonly int ReadonlyField = 0;
		public int ReadonlyProperty { get; } = 0;

	}

	partial struct PartialStructWithFieldsAndMembers { }

}

namespace D2L.CodeStyle.Analyzers.Specs.Sad {

	public struct StructWithFieldsAndMembers {

		public int Field = 0;
		public int Property { get; set; } = 0;

	}

}

namespace D2L.CodeStyle.Analyzers.Specs.Bad {

	public struct /* StructShouldBeReadonly(EmptyStruct) */ EmptyStruct /**/ { }

	public struct /* StructShouldBeReadonly(StructWithFieldsAndMembers) */ StructWithFieldsAndMembers /**/ {

		public const int ConstField = 0;
		public readonly int ReadonlyField = 0;
		public int ReadonlyProperty { get; } = 0;

	}

	public partial struct /* StructShouldBeReadonly(PartialStructWithFieldsAndMembers) */ PartialStructWithFieldsAndMembers /**/ {

		public const int ConstField = 0;
		public readonly int ReadonlyField = 0;
		public int ReadonlyProperty { get; } = 0;

	}

	partial struct PartialStructWithFieldsAndMembers {}

}
