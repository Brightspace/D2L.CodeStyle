using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace D2L.CodeStyle.Analysis {
<<<<<<< HEAD
    public static class TypeSymbolExtensions {
        private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

        public static string GetFullTypeName( this ITypeSymbol symbol ) {
            var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
            return fullyQualifiedName;
        }


        public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
            var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );

            var generics = symbol.GetGenericArguments();
            if( generics != null && generics.Any() ) {
                return string.Format(
                    "{0}<{1}>",
                    fullyQualifiedName,
                    string.Join( ",", generics.Select( g => g.GetFullTypeNameWithGenericArguments() ) )
                );
            }

            return fullyQualifiedName;
        }

        public static IEnumerable<ITypeSymbol> GetGenericArguments( this ITypeSymbol type ) {
            var namedType = type as INamedTypeSymbol;
            if( namedType == null ) {
                // problem getting generic type argument
                return null;
            }

            var args = namedType.TypeArguments;
            return args;
        }
    }

    public static class SyntaxNodeExtension {
        public static bool IsPropertyGetterImplemented(this PropertyDeclarationSyntax syntax) {
            var getter = syntax.AccessorList?.Accessors.FirstOrDefault( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) );
            return getter?.Body != null;
        }
    }
=======
	public static class TypeSymbolExtensions {
		private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
		);

		public static string GetFullTypeName( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
			return fullyQualifiedName;
		}


		public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );

			var generics = symbol.GetGenericArguments();
			if( generics != null && generics.Any() ) {
				return string.Format(
					"{0}<{1}>",
					fullyQualifiedName,
					string.Join( ",", generics.Select( g => g.GetFullTypeNameWithGenericArguments() ) )
				);
			}

			return fullyQualifiedName;
		}

		public static IEnumerable<ITypeSymbol> GetGenericArguments( this ITypeSymbol type ) {
			var namedType = type as INamedTypeSymbol;
			if( namedType == null ) {
				// problem getting generic type argument
				return null;
			}

			var args = namedType.TypeArguments;
			return args;
		}

		public static IEnumerable<ISymbol> GetNonStaticMembers( this INamespaceOrTypeSymbol type ) {

			return type.GetMembers()
				.Where( t => !t.IsStatic );
		}
	}
>>>>>>> master
}