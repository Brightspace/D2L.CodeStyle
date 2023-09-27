using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Pinning {
	public static class PinnedAnalyzerHelper {

		public const string PinnedAttributeName = "D2L.CodeStyle.Annotations.Pinning.PinnedAttribute";

		public static bool TryGetPinnedAttribute( ISymbol classSymbol, INamedTypeSymbol pinnedAttributeSymbol, out AttributeData? attribute ) {
			attribute = null;

			foreach( AttributeData? attributeData in classSymbol.GetAttributes() ) {
				INamedTypeSymbol? attributeSymbol = attributeData.AttributeClass;
				if( pinnedAttributeSymbol.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) {
					attribute = attributeData;
					return true;
				}
			}

			return false;
		}
	}
}
