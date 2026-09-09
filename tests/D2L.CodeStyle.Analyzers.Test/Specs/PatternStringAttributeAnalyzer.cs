// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.PatternStringAttributeAnalyzer, D2L.CodeStyle.Analyzers.Rule.PatternString

using System;

namespace SpecTests {

	using D2L.CodeStyle.Annotations.Contract;

	public readonly struct Digits {
		private Digits( string s, bool @private ) {}

		public Digits( [PatternString( "^[0-9]+$" )] string s ) : this( s, true ) {}

		public static implicit operator Digits( [PatternString( "^[0-9]+$" )] string s )
			=> new( s, true );
	}

	// Multiple [PatternString] attributes require the value to satisfy every pattern
	public readonly struct ThreeDigits {
		private ThreeDigits( string s, bool @private ) { }

		public ThreeDigits( [PatternString( "^[0-9]+$" )][PatternString( "^.{3}$" )] string s ) : this( s, true ) { }

		public static implicit operator ThreeDigits( [PatternString( "^[0-9]+$" )][PatternString( "^.{3}$" )] string s )
			=> new( s, true );
	}

	// ExpectMatch = false requires a constatns tring that does NOT match
	public readonly struct NoDigits {
		private NoDigits( string s, bool @private ) { }

		public NoDigits( [PatternString( "^[0-9]+$", ExpectMatch = false )] string s ) : this( s, true ) { }

		public static implicit operator NoDigits( [PatternString( "^[0-9]+$", ExpectMatch = false )] string s )
			=> new( s, true );
	}

	public sealed class NonString {
		public static void M( [/* PatternStringOnNonStringType(int) */ PatternString( "^[0-9]+$" ) /**/] int value ) { }
	}

	public sealed class Tests {

		void SinglePatternTests() {

			const string DIGITS = "123";
			const string LETTERS = "abc";

			#region Matching values are fine
			Digits _ = DIGITS;
			Digits _ = new( DIGITS );
			_ = (Digits)DIGITS;

			Digits _ = "123";
			Digits _ = new( "123" );
			_ = (DIGITS)"123";

			Digits _ = DIGITS + "456";
			Digits _ = new( DIGITS + "456" );
			_ = (Digits)( DIGITS + "456" );
			#endregion

			#region Non-matching values are flagged
			Digits _ = /* PatternStringDoesNotMatch(12a, to match, ^[0-9]+$) */ "12a" /**/;
			Digits _ = new( /* PatternStringDoesNotMatch(12a, to match, ^[0-9]+$) */ "12a" /**/ );
			_ = (Digits) /* PatternStringDoesNotMatch(12a, to match, ^[0-9]+$) */ "12a" /**/;
			#endregion

			#region ExpectMatch = false semantics
			NoDigits _ = "abc";
			NoDigits _ = new( "abc" );
			_ = (NoDigits)"abc";
			NoDigits _ = /* PatternStringDoesNotMatch(123, to not match, ^[0-9]+$) */ "123" /**/;
			NoDigits _ = new( /* PatternStringDoesNotMatch(123, to not match, ^[0-9]+$) */ "123" /**/ );
			_ = (NoDigits) /* PatternStringDoesNotMatch(123, to not match, ^[0-9]+$) */ "123" /**/
			#endregion
		}

		void NonConstantValuesAreFlagged() {

			string variable = "123";

			#region Non-constant values are flagged as needing to be constant
			Digits _ = new( /* PatternStringMustBeConstant() */ variable /**/ );
			Digits _ = /* PatternStringMustBeConstant() */ variable /**/;
			_ = (Digits)/* PatternStringMustBeConstant() */ variable /**/;
			#endregion
		}

		void MultiplePatternTests() {

			#region A value satisfying every pattern is fine
			ThreeDigits _ = "123";
			ThreeDigits _ = new( "123" );
			_ = (ThreeDigits)"123";
			#endregion

			#region Failing the first pattern (not digits) is flagged
			ThreeDigits _ = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			ThreeDigits _ = new( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/ );
			_ = (ThreeDigits) /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			#endregion

			#region Failing the second pattern (wrong length) is flagged
			ThreeDigits _ = /* PatternStringDoesNotMatch(12, to match, ^.{3}$) */ "12" /**/;
			ThreeDigits _ = new( /* PatternStringDoesNotMatch(12, to match, ^.{3}$) */ "12" /**/ );
			_ = (ThreeDigits) /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			#endregion

			#region Failing both patterns is flagged once per pattern
			ThreeDigits _  = /* PatternStringDoesNotMatch(ab, to match, ^[0-9]+$) | PatternStringDoesNotMatch(ab, to match, ^.{3}$) */ "ab" /**/;
			ThreeDigits _  = new( /* PatternStringDoesNotMatch(ab, to match, ^[0-9]+$) | PatternStringDoesNotMatch(ab, to match, ^.{3}$) */ "ab" /**/ );
			_ = (ThreeDigits) /* PatternStringDoesNotMatch(ab, to match, ^[0-9]+$) | PatternStringDoesNotMatch(ab, to match, ^.{3}$) */ "ab" /**/;
			#endregion
		}
	}

}
