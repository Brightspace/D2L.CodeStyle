using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Security.AccessControl;

namespace D2L.CodeStyle.Analyzers.Common {

	public static class SyntaxNodeExtension {

		public static bool IsAutoImplemented( this PropertyDeclarationSyntax syntax ) {
			// Remember: auto properties are the ones that hold state. Other
			// properties are just sugar around function calls.

			// For expression bodied properties like
			//   public MyType MyProp => expr;
			// expr is invoked for every access so this behaves the same as
			//   public MyType MyProp { get { return expr; } }

			// Conveniently enough expr is stored in the initializer field of
			// the PropertyDeclarationSyntax, so the code for analyzing fields
			// will narrow the inspected type to that of expr.
			if ( syntax.AccessorList == null ) {
				return false;
			}

			// Auto-implemented properties have at least an implicit (no body)
			// get and never have an explicit set.
			foreach( var accessor in syntax.AccessorList.Accessors ) {
				if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration
			 	 || accessor.Kind() == SyntaxKind.SetAccessorDeclaration
				) {
					if ( accessor.Body != null ) {
						return false;
					}
				}
			}

			return true;
		}
	}

	public static class TypeSymbolExtensions {

		private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		private static readonly SymbolDisplayFormat FullTypeWithGenericsDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		public static string GetFullTypeName( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
			return fullyQualifiedName;
		}


		public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeWithGenericsDisplayFormat );
			return fullyQualifiedName;
		}

		public static IEnumerable<ISymbol> GetExplicitNonStaticMembers( this ITypeSymbol type ) {

			return type.GetMembers()
				.Where( t => !t.IsStatic && !t.IsImplicitlyDeclared );
		}

		public static bool IsNullOrErrorType( this ITypeSymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}
			if( symbol.TypeKind == TypeKind.Error ) {
				return true;
			}

			return false;
		}

		public static bool IsNullOrErrorType( this ISymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}

			return false;
		}

	}
}