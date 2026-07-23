// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.PatternStringAttributeAnalyzer, D2L.CodeStyle.Analyzers.Rule.PatternString

using System;

namespace SpecTests {

	using D2L.CodeStyle.Annotations.Contract;

	public sealed class Model {

		// Requires a constant string consisting solely of digits.
		// [Constant] is no longer required alongside [PatternString].
		[PatternString( "^[0-9]+$" )]
		public string Property { get; set; }

		[PatternString( "^[0-9]+$" )]
		public string Field;

		// Requires a constant string that does NOT contain whitespace.
		[PatternString( "\\s", expectMatch: false )]
		public string NoWhitespace { get; set; }

		// A const declaration is evaluated at its initializer.
		[PatternString( "^[0-9]+$" )]
		public const string GoodConst = "123";

		[PatternString( "^[0-9]+$" )]
		public const string BadConst = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;

		// A static field declaration is evaluated at its initializer.
		[PatternString( "^[0-9]+$" )]
		public static readonly string GoodStaticField = "123";

		[PatternString( "^[0-9]+$" )]
		public static readonly string BadStaticField = /* PatternStringDoesNotMatch(xyz, to match, ^[0-9]+$) */ "xyz" /**/;

		// Multiple [PatternString] attributes require the value to satisfy every
		// pattern: must be digits AND must be exactly 3 characters long.
		[PatternString( "^[0-9]+$" )]
		[PatternString( "^.{3}$" )]
		public string MultiPattern { get; set; }

		[PatternString( "^[0-9]+$" )]
		[PatternString( "^.{3}$" )]
		public const string GoodMultiConst = "123";
	}

	public sealed class Tests {

		void PropertyAndFieldTests() {

			const string DIGITS = "123";
			const string LETTERS = "abc";

			#region Matching values are fine
			var good = new Model {
				Property = "123",
				Field = DIGITS
			};

			good.Property = "123";
			good.Property = DIGITS;
			good.Field = DIGITS + "456";
			#endregion

			#region Non-matching values are flagged
			good.Property = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			good.Property = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ LETTERS /**/;
			good.Field = /* PatternStringDoesNotMatch(12a, to match, ^[0-9]+$) */ "12a" /**/;
			#endregion

			#region expectMatch: false semantics (must NOT contain whitespace)
			good.NoWhitespace = "no-whitespace";
			good.NoWhitespace = /* PatternStringDoesNotMatch(has space, to not match, \s) */ "has space" /**/;
			#endregion
		}

		void NonConstantValuesAreFlagged() {

			string variable = "123";
			var good = new Model();

			#region Non-constant values are flagged as needing to be constant
			good.Property = /* PatternStringMustBeConstant() */ variable /**/;
			good.Field = /* PatternStringMustBeConstant() */ Guid.NewGuid().ToString() /**/;

			var built = new Model {
				Property = /* PatternStringMustBeConstant() */ variable /**/
			};
			#endregion
		}

		void InitializerTests() {

			#region Non-matching initializers are flagged
			var bad = new Model {
				Property = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/,
				Field = /* PatternStringDoesNotMatch(xyz, to match, ^[0-9]+$) */ "xyz" /**/
			};
			#endregion
		}

		void MultiplePatternTests() {

			var model = new Model();

			#region A value satisfying every pattern is fine
			model.MultiPattern = "123";
			#endregion

			#region Failing the first pattern (not digits) is flagged
			model.MultiPattern = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			#endregion

			#region Failing the second pattern (wrong length) is flagged
			model.MultiPattern = /* PatternStringDoesNotMatch(12, to match, ^.{3}$) */ "12" /**/;
			#endregion

			#region Failing both patterns is flagged once per pattern
			model.MultiPattern = /* PatternStringDoesNotMatch(ab, to match, ^[0-9]+$) | PatternStringDoesNotMatch(ab, to match, ^.{3}$) */ "ab" /**/;
			#endregion
		}
	}
}
