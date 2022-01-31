using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Extensions {

	[TestFixture]
	internal sealed class SymbolExtensionsTests {

		[Test]
		public void GetAllContainingTypes_WhenNone() {

			ISymbol symbol = MockContainmentSymbol( containingType: null );

			ImmutableArray<INamedTypeSymbol> containingTypes = RoslynExtensions
				.GetAllContainingTypes( symbol );

			Assert.That( containingTypes, Is.Empty );
		}

		[Test]
		public void GetAllContainingTypes_WhenOne() {

			INamedTypeSymbol root = MockContainmentSymbol( containingType: null );
			ISymbol symbol = MockContainmentSymbol( containingType: root );

			ImmutableArray<INamedTypeSymbol> containingTypes = RoslynExtensions
				.GetAllContainingTypes( symbol );

			Assert.That( containingTypes, Is.EqualTo( new[] { root } ) );
		}

		[Test]
		public void GetAllContainingTypes_WhenMany() {

			INamedTypeSymbol root = MockContainmentSymbol( containingType: null );
			INamedTypeSymbol trunk = MockContainmentSymbol( containingType: root );
			INamedTypeSymbol branch = MockContainmentSymbol( containingType: trunk );
			ISymbol symbol = MockContainmentSymbol( containingType: branch );

			ImmutableArray<INamedTypeSymbol> containingTypes = RoslynExtensions
				.GetAllContainingTypes( symbol );

			Assert.That(
					containingTypes,
					Is.EqualTo( new[] { root, trunk, branch } ),
					"Should return types in order, deepest last"
				);
		}

		private static INamedTypeSymbol MockContainmentSymbol(
				INamedTypeSymbol containingType
			) {

			Mock<INamedTypeSymbol> symbol = new( MockBehavior.Strict );
			symbol
				.Setup( s => s.ContainingType )
				.Returns( containingType );

			return symbol.Object;
		}
	}
}
