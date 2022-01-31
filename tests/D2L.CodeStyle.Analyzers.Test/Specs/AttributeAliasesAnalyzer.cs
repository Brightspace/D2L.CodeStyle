// analyzer: D2L.CodeStyle.Analyzers.Language.AttributeAliasesAnalyzer

using System;

using static Test.Attributes.Container;
using RootAliasAttribute = Test.Attributes.TestAttribute;

namespace Test.Attributes {

	[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
	public sealed class TestAttribute : Attribute { }

	public static class Container {

		[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
		public sealed class InnerAttribute : Attribute { }
	}
}

namespace Test.Cases {

	namespace Aliases {

		using Test.Attributes;
		using ShortAlias = Test.Attributes.TestAttribute;
		using LongAliasAttribute = Test.Attributes.TestAttribute;
		using InnerAliasAttribute = InnerAttribute;

		[ /* AliasingAttributeNamesNotSupported() */ ShortAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ LongAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ LongAliasAttribute /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ RootAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ RootAliasAttribute /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ InnerAlias /**/ ]
		[ /* AliasingAttributeNamesNotSupported() */ InnerAliasAttribute /**/ ]
		public sealed class Usage { }

		// Verifies that all namespaces are checked
		namespace Nested {

			[ /* AliasingAttributeNamesNotSupported() */ ShortAlias /**/ ]
			[ /* AliasingAttributeNamesNotSupported() */ LongAlias /**/ ]
			[ /* AliasingAttributeNamesNotSupported() */ LongAliasAttribute /**/ ]
			public sealed class Usage { }
		}
	}

	namespace ImportAliases {

		using TestAttribute = Test.Attributes.TestAttribute;
		using InnerAttribute = Test.Attributes.Container.InnerAttribute;

		[Test]
		[TestAttribute]
		[Inner]
		[InnerAttribute]
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
		[Inner]
		[InnerAttribute]
		[Container.Inner]
		[Container.InnerAttribute]
		[Test.Attributes.Container.Inner]
		[Test.Attributes.Container.InnerAttribute]
		public sealed class Usage { }
	}
}
