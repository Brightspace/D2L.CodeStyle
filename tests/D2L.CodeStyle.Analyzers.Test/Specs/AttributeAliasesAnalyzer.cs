// analyzer: D2L.CodeStyle.Analyzers.Language.AttributeAliasesAnalyzer

using System;

namespace Test.Attributes {

	[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
	public sealed class TestAttribute : Attribute { }
}

namespace Test.Cases {

	namespace Aliases {

		using Test.Attributes;
		using ShortAlias = Test.Attributes.TestAttribute;
		using LongAliasAttribute = Test.Attributes.TestAttribute;

		[ /* AliasingAttributeNamesNotSupported() */ ShortAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ LongAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ LongAliasAttribute /**/ ]
		public sealed class Usage { }
	}

	namespace ImportAliases {

		using TestAttribute = Test.Attributes.TestAttribute;

		[Test]
		[TestAttribute]
		public sealed class Usage { }
	}

	namespace NonAliases {

		using Test.Attributes;

		[Test]
		[TestAttribute]
		[Attributes.Test]
		[Attributes.TestAttribute]
		[Test.Attributes.Test]
		[Test.Attributes.TestAttribute]
		public sealed class Usage { }
	}
}
