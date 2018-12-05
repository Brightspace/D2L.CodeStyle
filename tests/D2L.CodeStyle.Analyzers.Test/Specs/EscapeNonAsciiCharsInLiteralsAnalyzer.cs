// analyzer: D2L.CodeStyle.Analyzers.Language.EscapeNonAsciiCharsInLiteralsAnalyzer

namespace D2L.CodeStyle.Analyzers.Specs {
	public static class Tests {
		const string EmptyString = "";
		const string SingleCharString = "x";
		const string BigString = "this is a string literal\a with \"lots\" of ASCII \041\x21! It even has escaped unicode like \u03c0 = 3.14159...";

		const string StartsWithNonAscii =
			/* EscapeNonAsciiCharsInLiteral(string,"\u03C0 = 3.14159...") */ "π = 3.14159..." /**/;

		const string NoAscii =
			/* EscapeNonAsciiCharsInLiteral(string,"\u03B1\u03B2\u03B3") */ "αβγ" /**/;

		const string ANiceMix =
			/* EscapeNonAsciiCharsInLiteral(string,"alpha \u03B1 beta \u03B2 alpha-beta \u03B1\u03B2 gamma \u03B3 alpha-beta-gamma \u03B1\u03B2\u03B3.") */ "alpha α beta β alpha-beta αβ gamma γ alpha-beta-gamma αβγ." /**/;

		const string TableFlip =
			/* EscapeNonAsciiCharsInLiteral(string,"(\u256F\u00B0\u25A1\u00B0\uFF09\u256F\uFE35 \u253B\u2501\u253B") */ "(╯°□°）╯︵ ┻━┻" /**/;

		const string Brail =
			/* EscapeNonAsciiCharsInLiteral(string,"\u284C\u2801\u2827\u2811 \u283C\u2801\u2812 \u284D\u281C\u2807\u2811\u2839\u2830\u280E \u2863\u2815\u280C") */ "⡌⠁⠧⠑ ⠼⠁⠒ ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌" /**/;

		// This one hits the branch for surrogate pairs
		const string MormonTwinkleTwinkleLittleStar =
			/* EscapeNonAsciiCharsInLiteral(string,"\U00010413\uDC13\U00010436\uDC36\U0001042E\uDC2E\U0001044D\uDC4D\U0001043F\uDC3F\U0001044A\uDC4A \U0001043B\uDC3B\U00010436\uDC36\U0001042E\uDC2E\U0001044D\uDC4D\U0001043F\uDC3F\U0001044A\uDC4A \U0001044A\uDC4A\U0001042E\uDC2E\U0001043B\uDC3B\U0001044A\uDC4A \U00010445\uDC45\U0001043B\uDC3B\U0001042A\uDC2A\U00010449\uDC49") */ "𐐓𐐶𐐮𐑍𐐿𐑊 𐐻𐐶𐐮𐑍𐐿𐑊 𐑊𐐮𐐻𐑊 𐑅𐐻𐐪𐑉" /**/;

		const string Emojis =
			/* EscapeNonAsciiCharsInLiteral(string,"\U0001F602\uDE02\U0001F60D\uDE0D\U0001F389\uDF89\U0001F44D\uDC4D") */ "😂😍🎉👍" /**/;

		const char AsciiChar = 'x';
		const char OtherAsciiChar = '\x21';
		const char Japanese = /* EscapeNonAsciiCharsInLiteral(char,'\u3041') */ 'ぁ' /**/;
		const char EscapedChar = '\u3041';

		// TODO: this is ignored. We need to implement this:
		// https://github.com/dotnet/codeformatter/issues/39
		const string VerbatimString = @"Would you like to build a ☃?";
	}
}
