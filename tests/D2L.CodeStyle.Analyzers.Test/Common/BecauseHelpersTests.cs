using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {
	[TestFixture]
	internal sealed class BecauseHelpersTests {

		private const string s_preamble = @"
using System;
using D2L.CodeStyle.Annotations;
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

	public enum Because {
		ItHasntBeenLookedAt = 1,
		ItsSketchy = 2,
		ItsStickyDataOhNooo = 3,
		WeNeedToMakeTheAnalyzerConsiderThisSafe = 4,
		ItsUgly = 5,
		ItsOnDeathRow = 6
	}

	public static partial class Mutability {
		public sealed class UnauditedAttribute : Attribute {
			public readonly Because m_cuz;

			public UnauditedAttribute( Because why ) {
				m_cuz = why;
			}
		}
	}
}
";

		[Test]
		public void GetImmutabilityExceptions_WhenTypeDoesNotHaveImmutableAttribute_ReturnsAllReasons() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"public class Foo { }" );

			IImmutableSet<Because> immutabilityExceptions = BecauseHelpers.GetImmutabilityExceptions( ty.Symbol );

			Assert.That( immutabilityExceptions, Is.EquivalentTo( Enum.GetValues( typeof( Because ) ) ) );
		}

		[Test]
		public void GetImmutabilityExceptions_WhenNoSpecifiedAllowedUnauditedReasons_ReturnsAllReasons() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable]
public class Foo { }
" );

			IImmutableSet<Because> immutabilityExceptions = BecauseHelpers.GetImmutabilityExceptions( ty.Symbol );

			Assert.That( immutabilityExceptions, Is.EquivalentTo( Enum.GetValues( typeof( Because ) ) ) );
		}

		[Test]
		public void GetImmutabilityExceptions_WhenSpecifiedAllowedUnauditedReasons_ReturnsSpecifiedReasons() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItsUgly | Except.WeNeedToMakeTheAnalyzerConsiderThisSafe )]
public class Foo { }
" );

			IImmutableSet<Because> immutabilityExceptions = BecauseHelpers.GetImmutabilityExceptions( ty.Symbol );

			Assert.That( immutabilityExceptions, Is.EquivalentTo( new[] {
				Because.WeNeedToMakeTheAnalyzerConsiderThisSafe,
				Because.ItsUgly
			} ) );
		}

		[Test]
		public void GetImmutabilityExceptions_WhenSpecifiedNoneReasons_ReturnsEmptySet() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.None )]
public class Foo { }
" );

			IImmutableSet<Because> immutabilityExceptions = BecauseHelpers.GetImmutabilityExceptions( ty.Symbol );

			Assert.That( immutabilityExceptions, Is.Empty );
		}

		[Test]
		public void TryGetUnauditedReason_WhenSymbolDoesNotHaveUnauditedAttribute_ReturnsFalse() {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( @"
public class Foo {
	public string foo;
}
" );

			Because reason;
			bool result = BecauseHelpers.TryGetUnauditedReason( field.Symbol, out reason );

			Assert.That( result, Is.False );
		}

		[TestCaseSource( nameof( GetBecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReason_ReturnsTrueAndCorrectReason( Because expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( Because.{Enum.GetName( typeof( Because ), expectedReason )} )]
	public string foo;
}}
" );

			Because reason;
			bool result = BecauseHelpers.TryGetUnauditedReason( field.Symbol, out reason );

			Assert.That( result, Is.True );
			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		private static IEnumerable<Because> GetBecauseReasons() {
			return Enum.GetValues( typeof( Because ) ).Cast<Because>();
		}

		private static TestSymbol<ITypeSymbol> CompileAndGetFooType( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = s_preamble + source;

			var compilation = Compile( source );
			var symbol = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( symbol );
			Assert.AreNotEqual( TypeKind.Error, symbol.TypeKind );

			return new TestSymbol<ITypeSymbol>( symbol, compilation );
		}

		private static TestSymbol<IFieldSymbol> CompileAndGetFooField( string source ) {
			TestSymbol<ITypeSymbol> fooType = CompileAndGetFooType( source );

			IFieldSymbol fooField = fooType.Symbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault( s => s.Name == "foo" );

			return new TestSymbol<IFieldSymbol>( fooField, fooType.Compilation );
		}

	}
}
