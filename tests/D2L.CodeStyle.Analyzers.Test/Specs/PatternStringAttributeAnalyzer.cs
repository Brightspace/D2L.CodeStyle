// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.PatternStringAttributeAnalyzer, D2L.CodeStyle.Analyzers.Rule.PatternString

using System;

namespace SpecTests {

	using D2L.CodeStyle.Annotations.Contract;

	public sealed class Model {

		// Requires a constant string consisting solely of digits.
		[Constant]
		[PatternString( "^[0-9]+$" )]
		public string Property { get; set; }

		[Constant]
		[PatternString( "^[0-9]+$" )]
		public string Field;

		// Requires a constant string that does NOT contain whitespace.
		[Constant]
		[PatternString( "\\s", expectMatch: false )]
		public string NoWhitespace { get; set; }

		// [PatternString] without [Constant] is flagged on the member declaration.
		[PatternString( "^[0-9]+$" )]
		public string /* PatternStringMustBeConstant() */ MissingConstant /**/ = "123";

		// A fully-attributed const declaration is evaluated at its initializer.
		[Constant]
		[PatternString( "^[0-9]+$" )]
		public const string GoodConst = "123";

		[Constant]
		[PatternString( "^[0-9]+$" )]
		public const string BadConst = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;

		// A fully-attributed static field declaration is evaluated at its initializer.
		[Constant]
		[PatternString( "^[0-9]+$" )]
		public static readonly string GoodStaticField = "123";

		[Constant]
		[PatternString( "^[0-9]+$" )]
		public static readonly string BadStaticField = /* PatternStringDoesNotMatch(xyz, to match, ^[0-9]+$) */ "xyz" /**/;
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

		void InitializerTests() {

			#region Non-matching initializers are flagged
			var bad = new Model {
				Property = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/,
				Field = /* PatternStringDoesNotMatch(xyz, to match, ^[0-9]+$) */ "xyz" /**/
			};
			#endregion
		}
	}
}
