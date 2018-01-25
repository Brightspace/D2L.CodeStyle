using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	internal sealed class BecauseHelpersTests {

		private const string s_preamble = @"
using System;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;
 
namespace D2L.CodeStyle.Annotations {
	public enum Because {
		ItHasntBeenLookedAt,
		ItsSketchy,
		ItsStickyDataOhNooo,
		WeNeedToMakeTheAnalyzerConsiderThisSafe,
		ItsUgly,
		ItsOnDeathRow
	}

	public enum UndiffBucket {
		Core
	}
 
	public static partial class Mutability {
		public sealed class UnauditedAttribute : Attribute {
 
			public UnauditedAttribute( Because why ) { }

			public UnauditedAttribute( Because why, UndiffBucket bucket ) { }
		}
	}
}
";

		[Test]
		public void TryGetUnauditedReason_WhenSymbolDoesNotHaveUnauditedAttribute_ThrowsException() {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( @"
public class Foo {
	public string foo;
}
" );

			Assert.That( () => BecauseHelpers.GetUnauditedReason( field.Symbol ), Throws.Exception );
		}

		private static readonly string[] BecauseReasons = {
			"ItHasntBeenLookedAt",
			"ItsSketchy",
			"ItsStickyDataOhNooo",
			"WeNeedToMakeTheAnalyzerConsiderThisSafe",
			"ItsUgly",
			"ItsOnDeathRow"
		};

		[TestCaseSource( nameof( BecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReason_ReturnsCorrectReason( string expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( Because.{expectedReason} )]
	public string foo;
}}
" );

			string reason = BecauseHelpers.GetUnauditedReason( field.Symbol );

			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		[TestCaseSource( nameof( BecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReason_UsingNamedParameters_ReturnsCorrectReason( string expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( why: Because.{expectedReason} )]
	public string foo;
}}
" );

			string reason = BecauseHelpers.GetUnauditedReason( field.Symbol );

			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		[TestCaseSource( nameof( BecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReasonAndHasBucket_ReturnsCorrectReason( string expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( Because.{expectedReason}, UndiffBucket.Core )]
	public string foo;
}}
" );

			string reason = BecauseHelpers.GetUnauditedReason( field.Symbol );

			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		[TestCaseSource( nameof( BecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReasonAndHasBucket_UsingNamedParameters_ReturnsCorrectReason( string expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( why: Because.{expectedReason}, bucket: UndiffBucket.Core )]
	public string foo;
}}
" );

			string reason = BecauseHelpers.GetUnauditedReason( field.Symbol );

			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		[TestCaseSource( nameof( BecauseReasons ) )]
		public void TryGetUnauditedReason_WhenSymbolIsAnnotatedWithUnauditedReasonAndHasBucket_UsingNamedParametersWithReversedOrder_ReturnsCorrectReason( string expectedReason ) {
			TestSymbol<IFieldSymbol> field = CompileAndGetFooField( $@"
public class Foo {{
	[Mutability.Unaudited( bucket: UndiffBucket.Core, why: Because.{expectedReason} )]
	public string foo;
}}
" );

			string reason = BecauseHelpers.GetUnauditedReason( field.Symbol );

			Assert.That( reason, Is.EqualTo( expectedReason ) );
		}

		private static TestSymbol<IFieldSymbol> CompileAndGetFooField( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = s_preamble + source;

			var compilation = Compile( source );
			var symbol = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( symbol );
			Assert.AreNotEqual( TypeKind.Error, symbol.TypeKind );
			TestSymbol<ITypeSymbol> fooType = new TestSymbol<ITypeSymbol>( symbol, compilation );

			IFieldSymbol fooField = fooType.Symbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault( s => s.Name == "foo" );

			return new TestSymbol<IFieldSymbol>( fooField, fooType.Compilation );
		}

	}
}