using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analysis {
    public static class TypeSymbolExtensions {
        private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

        private static readonly ImmutableHashSet<string> ImmutableCollectionTypes = new HashSet<string> {
            "System.Collections.Immutable.ImmutableArray",
            "System.Collections.Immutable.ImmutableDictionary",
            "System.Collections.Immutable.ImmutableHashSet",
            "System.Collections.Immutable.ImmutableList",
            "System.Collections.Immutable.ImmutableQueue",
            "System.Collections.Immutable.ImmutableSortedDictionary",
            "System.Collections.Immutable.ImmutableSortedSet",
            "System.Collections.Immutable.ImmutableStack",
            "System.Collections.Generic.IReadOnlyList",
            "System.Collections.Generic.IEnumerable",
        }.ToImmutableHashSet();

        public static string GetFullTypeName( this ITypeSymbol symbol ) {
            var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
            return fullyQualifiedName;
        }

        public static bool IsImmutableCollectionType( this ITypeSymbol type ) {
            return ImmutableCollectionTypes.Contains( type.GetFullTypeName() );
        }

        public static ITypeSymbol GetCollectionElementType( this ITypeSymbol type ) {
            var namedType = type as INamedTypeSymbol;
            if( namedType == null ) {
                // problem getting generic type argument
                return null;
            }

            var collectionElementType = namedType.TypeArguments;
            if( collectionElementType.IsEmpty ) {
                // we're looking at a non-generic collection -- it cannot be deterministically immutable
                return null;
            }
            if( collectionElementType.Length > 1 ) {
                // collections should only have one; if we have > 1, this isn't a collection
                return null;
            }

            return collectionElementType[0];
        }

    }
}