using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Extensions {

	[TestFixture]
	internal sealed class NameSyntaxExtensionsTests {

		private static IEnumerable<GetUnqualifiedNameAsStringTestCase> GetUnqualifiedNameAsStringTestCases() {

			yield return new GetUnqualifiedNameAsStringTestCase(
				testCaseName: nameof( GenericNameSyntax ),
				nameSyntax: SyntaxFactory.GenericName(
					SyntaxFactory.Identifier( "List" ),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SeparatedList<TypeSyntax>( new[] {
							SyntaxFactory.IdentifierName( "string" )
						} )
					)
				),
				expectedResult: "List<string>"
			);

			yield return new GetUnqualifiedNameAsStringTestCase(
				testCaseName: nameof( IdentifierNameSyntax ),
				nameSyntax: SyntaxFactory.IdentifierName( "DateTime" ),
				expectedResult: "DateTime"
			);

			yield return new GetUnqualifiedNameAsStringTestCase(
				testCaseName: nameof( QualifiedNameSyntax ),
				nameSyntax: SyntaxFactory.QualifiedName(
					SyntaxFactory.IdentifierName( "System" ),
					SyntaxFactory.IdentifierName( "TimeSpan" )
				),
				expectedResult: "TimeSpan"
			);

			yield return new GetUnqualifiedNameAsStringTestCase(
				testCaseName: nameof( AliasQualifiedNameSyntax ),
				nameSyntax: SyntaxFactory.AliasQualifiedName(
					SyntaxFactory.IdentifierName( "LibraryV2" ),
					SyntaxFactory.IdentifierName( "Foo" )
				),
				expectedResult: "Foo"
			);
		}

		[Test]
		[TestCaseSource( nameof( GetUnqualifiedNameAsStringTestCases ) )]
		public string GetUnqualifiedNameAsString( NameSyntax nameSyntax ) {
			return nameSyntax.GetUnqualifiedNameAsString();
		}

		private sealed class GetUnqualifiedNameAsStringTestCase : TestCaseData {

			public GetUnqualifiedNameAsStringTestCase(
					string testCaseName,
					NameSyntax nameSyntax,
					string expectedResult
				)
				: base( nameSyntax ) {

				this.SetName( testCaseName );
				this.Returns( expectedResult );
			}
		}
	}
}
