using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal static class AssertIsBoolSymbols {

		public const string IsTrue = "NUnit.Framework.Assert.IsTrue";
		public const string IsFalse = "NUnit.Framework.Assert.IsFalse";

		private static readonly SymbolDisplayFormat MethodDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

		public static string GetSymbolName( ISymbol symbol ) {
			return symbol.ToDisplayString( MethodDisplayFormat );
		}

		public static bool IsMatch( string symbolName ) {
			return symbolName == IsTrue || symbolName == IsFalse;
		}
	}
}
