// analyzer: D2L.CodeStyle.Analyzers.Language.AttributeUnqualifiedNameAnalyzer

using System;

namespace Test.Attributes {

	[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
	public sealed class TestAttribute : Attribute { }
}

namespace Test.Cases {

	using Test.Attributes;

	namespace Verbose {

		[ /* ConciseAttributeName() */ TestAttribute /**/ ]
		[ /* ConciseAttributeName() */ Attributes.TestAttribute /**/ ]
		[ /* ConciseAttributeName() */ Test.Attributes.TestAttribute /**/ ]
		[ /* ConciseAttributeName() */ global::Test.Attributes.TestAttribute /**/ ]
		public sealed class Usage { }
	}

	namespace Concise {

		[Test]
		[Attributes.Test]
		[Test.Attributes.Test]
		[global::Test.Attributes.Test]
		public sealed class Usage { }
	}
}
