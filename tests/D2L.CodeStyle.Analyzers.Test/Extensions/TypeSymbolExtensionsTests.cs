using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Extensions {

	[TestFixture]
	internal sealed class TypeSymbolExtensionsTests {

		[Test]
		[TestCase( "string", "System.String", Description = "special non-generic type" )]
		[TestCase( "String", "System.String", Description = "non-special non-generic type" )]
		[TestCase( "Lazy<string>", "System.Lazy", Description = "special generic type" )]
		[TestCase( "Lazy<String>", "System.Lazy", Description = "non-special generic type" )]
		public void GetFullTypeName_ReturnsCorrectValue( string typeName, string expected ) {
			var type = Field( typeName + " name;" ).Symbol.Type;

			var actual = type.GetFullTypeName();

			Assert.AreEqual( expected, actual );
		}

		[Test]
		[TestCase( "string", "System.String", Description = "special non-generic type" )]
		[TestCase( "String", "System.String", Description = "non-special non-generic type" )]
		[TestCase( "Lazy<string>", "System.Lazy<System.String>", Description = "special generic type" )]
		[TestCase( "Lazy<String>", "System.Lazy<System.String>", Description = "non-special generic type" )]
		public void GetFullTypeNameWithGenericArguments_ReturnsCorrectValue( string typeName, string expected ) {
			var type = Field( typeName + " name;" ).Symbol.Type;

			var actual = type.GetFullTypeNameWithGenericArguments();

			Assert.AreEqual( expected, actual );
		}
	}
}
