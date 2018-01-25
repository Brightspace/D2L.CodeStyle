using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	internal sealed class ImmutableHelpersTests {

		private const string AnnotationsPreamble = @"
using System;
using static D2L.CodeStyle.Annotations.Objects;
 
namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public sealed class Immutable : Attribute {
 
			public Except Except { get; set; }
 
		}
 
		[Flags]
		public enum Except {
 
			None = 0,
			ItHasntBeenLookedAt = 1,
			ItsSketchy = 2,
			ItsStickyDataOhNooo = 4,
			WeNeedToMakeTheAnalyzerConsiderThisSafe = 8,
			ItsUgly = 16,
			ItsOnDeathRow = 32
 
		}
	}
}
";

		[Test]
		public void GetActualImmutableExceptions_WhenTypeDoesNotHaveImmutableAttribute_ThrowsException() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"public class Foo { }" );

			Assert.That( () => ty.Symbol.GetActualImmutableExceptions(), Throws.Exception );
		}

		[Test]
		public void GetActualImmutableExceptions_WhenNoSpecifiedExceptions_ReturnsDefaultReasons() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable]
public class Foo { }
" );

			IImmutableSet<string> immutabilityExceptions = ty.Symbol.GetActualImmutableExceptions();

			Assert.That( immutabilityExceptions, Is.EquivalentTo( ImmutableHelpers.DefaultImmutabilityExceptions ) );
		}

		[Test]
		public void GetActualImmutableExceptions_WhenSpecifiedExceptions_ReturnsSpecifiedExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItsUgly | Except.WeNeedToMakeTheAnalyzerConsiderThisSafe | Except.ItsSketchy )]
public class Foo { }
" );

			IImmutableSet<string> immutabilityExceptions = ty.Symbol.GetActualImmutableExceptions();

			Assert.That( immutabilityExceptions, Is.EquivalentTo( new[] {
				"WeNeedToMakeTheAnalyzerConsiderThisSafe",
				"ItsUgly",
				"ItsSketchy"
			} ) );
		}

		[Test]
		public void GetActualImmutableExceptions_WhenSpecifiedNoneReasons_ReturnsEmptySet() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.None )]
public class Foo { }
" );

			IImmutableSet<string> immutabilityExceptions = ty.Symbol.GetActualImmutableExceptions();

			Assert.That( immutabilityExceptions, Is.Empty );
		}

		private static TestSymbol<ITypeSymbol> CompileAndGetFooType( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = AnnotationsPreamble + source;

			var compilation = Compile( source );
			var symbol = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( symbol );
			Assert.AreNotEqual( TypeKind.Error, symbol.TypeKind );

			return new TestSymbol<ITypeSymbol>( symbol, compilation );
		}

	}
}
