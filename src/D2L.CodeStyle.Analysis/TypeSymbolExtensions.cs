using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analysis {
    public static class TypeSymbolExtensions {
        private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

        public static string GetFullTypeName( this ITypeSymbol symbol ) {
            var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
            return fullyQualifiedName;
        }

    }
}