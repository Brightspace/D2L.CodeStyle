using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace D2L.CodeStyle.Analyzers.Common {

	public static class SyntaxNodeExtension {
		public static bool IsPropertyGetterImplemented(this PropertyDeclarationSyntax syntax) {
			var getter = syntax.AccessorList?.Accessors.FirstOrDefault( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) );
			return getter?.Body != null;
		}
	}

	public static class TypeSymbolExtensions {
		private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
		);

		private static readonly SymbolDisplayFormat FullTypeWithGenericsDisplayFormat = new SymbolDisplayFormat( 
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, 
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters 
		);

		public static string GetFullTypeName( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
			return fullyQualifiedName;
		}


		public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeWithGenericsDisplayFormat );
			return fullyQualifiedName;
		}

		public static IEnumerable<ISymbol> GetNonStaticMembers( this INamespaceOrTypeSymbol type ) {

			return type.GetMembers()
				.Where( t => !t.IsStatic );
		}
	}
}