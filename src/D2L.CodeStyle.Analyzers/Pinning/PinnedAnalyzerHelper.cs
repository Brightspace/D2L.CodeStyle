using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Pinning {
	public static class PinnedAnalyzerHelper {

		public const string PinnedAttributeName = "D2L.CodeStyle.Annotations.Pinning.PinnedAttribute";

		public static AttributeData? GetPinnedAttribute( ISymbol classSymbol, INamedTypeSymbol pinnedAttributeSymbol ) {
			foreach( AttributeData? attributeData in classSymbol.GetAttributes() ) {
				INamedTypeSymbol? attributeSymbol = attributeData.AttributeClass;
				if( pinnedAttributeSymbol.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) {
					return attributeData;
				}
			}

			return null;
		}
	}
}
