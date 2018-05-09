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

		public static bool TryGetName( ISymbol symbol, out string symbolName ) {
			if( symbol == null ) {
				symbolName = null;
				return false;
			}

			symbolName = symbol.ToDisplayString( MethodDisplayFormat );
			if( symbolName == IsTrue || symbolName == IsFalse ) {
				return true;
			}

			symbolName = null;
			return false;
		}
	}
}
