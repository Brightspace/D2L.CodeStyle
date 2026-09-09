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
		[PatternString( "\\s", ExpectMatch = false )]
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

	public sealed class NonStringModel {

		// [PatternString] on a non-string member is flagged at the declaration.
		[/* PatternStringOnNonStringType(int) */ PatternString( "^[0-9]+$" ) /**/]
		public int NonStringProperty { get; set; }

		[/* PatternStringOnNonStringType(int) */ PatternString( "^[0-9]+$" ) /**/]
		public int NonStringField;

		// Every [PatternString] on a non-string member is flagged.
		[/* PatternStringOnNonStringType(int) */ PatternString( "^[0-9]+$" ) /**/]
		[/* PatternStringOnNonStringType(int) */ PatternString( "^.{3}$" ) /**/]
		public int MultiNonString { get; set; }

		// [PatternString] on a non-string parameter is flagged too.
		public static void NonStringParameter( [/* PatternStringOnNonStringType(int) */ PatternString( "^[0-9]+$" ) /**/] int value ) { }
	}

	public sealed class FieldList {

		// [PatternString] on a parameter validates the argument at the call site.
		public FieldList( [PatternString( "^[0-9]+$" )] string fields ) { }

		public static implicit operator FieldList( [PatternString( "^[0-9]+$" )] string fields ) {
			return null;
		}

		// [PatternString] may coexist with [Constant] on the same parameter.
		public static FieldList Create( [Constant][PatternString( "^[0-9]+$" )] string fields ) {
			return null;
		}
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

			#region ExpectMatch = false semantics (must NOT contain whitespace)
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

		void ParameterTests() {

			const string DIGITS = "123";
			string variable = "123";

			#region Matching constant arguments are fine
			var a = new FieldList( "123" );
			var b = new FieldList( DIGITS );
			var d = FieldList.Create( "123" );
			#endregion

			#region Non-matching constant arguments are flagged
			var e = new FieldList( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/ );
			var g = FieldList.Create( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/ );
			#endregion

			#region Non-constant arguments are flagged as needing to be constant
			var h = new FieldList( /* PatternStringMustBeConstant() */ variable /**/ );
			var i = new FieldList( /* PatternStringMustBeConstant() */ Guid.NewGuid().ToString() /**/ );
			#endregion
		}

		void ConversionTests() {

			const string DIGITS = "123";
			string variable = "123";

			#region Matching conversion operands are fine
			FieldList a = "123";
			FieldList b = DIGITS;
			var c = (FieldList)"123";
			#endregion

			#region Non-matching implicit conversion operands are flagged
			FieldList d = /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/;
			#endregion

			#region Non-matching explicit conversion operands are flagged
			var e = (FieldList)( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/ );
			#endregion

			#region Non-constant conversion operands are flagged as needing to be constant
			FieldList f = /* PatternStringMustBeConstant() */ variable /**/;
			#endregion
		}

		void DeconstructionTests() {

			var good = new Model();
			int number;
			string variable = "123";

			#region Matching values in a deconstruction are fine
			(good.Property, number) = ("123", 5);
			(good.Property, good.Field) = ("123", "456");
			#endregion

			#region Non-matching values in a deconstruction are flagged
			(good.Property, number) = ( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/, 5);
			(good.Property, good.Field) = ( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/, /* PatternStringDoesNotMatch(xyz, to match, ^[0-9]+$) */ "xyz" /**/ );
			#endregion

			#region Non-constant values in a deconstruction are flagged as needing to be constant
			(good.Property, number) = ( /* PatternStringMustBeConstant() */ variable /**/, 5);
			#endregion

			#region Nested deconstruction values are flagged
			((good.Property, number), good.Field) = (( /* PatternStringDoesNotMatch(abc, to match, ^[0-9]+$) */ "abc" /**/, 5), "123");
			#endregion
		}
	}

}
