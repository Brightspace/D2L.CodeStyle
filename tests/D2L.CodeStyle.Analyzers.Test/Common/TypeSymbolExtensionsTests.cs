using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {

	[TestFixture]
	internal sealed class TypeSymbolExtensionsTests {

		[Test]
		[TestCase( "string", "System.String", Description = "special non-generic type" )]
		[TestCase( "String", "System.String", Description = "non-special non-generic type" )]
		[TestCase( "Lazy<string>", "System.Lazy", Description = "special generic type" )]
		[TestCase( "Lazy<String>", "System.Lazy", Description = "non-special generic type" )]
		public void GetFullTypeName_ReturnsCorrectValue( string typeName, string expected ) {
			var type = Field( typeName + " name;" ).Type;

			var actual = TypeSymbolExtensions.GetFullTypeName( type );

			Assert.AreEqual( expected, actual );
		}

		[Test]
		[TestCase( "string", "System.String", Description = "special non-generic type" )]
		[TestCase( "String", "System.String", Description = "non-special non-generic type" )]
		[TestCase( "Lazy<string>", "System.Lazy<System.String>", Description = "special generic type" )]
		[TestCase( "Lazy<String>", "System.Lazy<System.String>", Description = "non-special generic type" )]
		public void GetFullTypeNameWithGenericArguments_ReturnsCorrectValue( string typeName, string expected ) {
			var type = Field( typeName + " name;" ).Type;

			var actual = TypeSymbolExtensions.GetFullTypeNameWithGenericArguments( type );

			Assert.AreEqual( expected, actual );
		}
	}
}
