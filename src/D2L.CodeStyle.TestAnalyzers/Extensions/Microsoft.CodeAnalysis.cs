using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.TestAnalyzers.Extensions {
	public static class RoslynExtensions {

		// Copied from the non-test assembly because we do not reference it.

		public static bool IsNullOrErrorType( this ITypeSymbol? symbol ) {

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

		public static bool IsErrorType( this ITypeSymbol symbol ) {

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

		public static ImmutableArray<INamedTypeSymbol> GetAllContainingTypes( this ISymbol symbol ) {

			INamedTypeSymbol containingType = symbol.ContainingType;
			if( containingType == null ) {
				return ImmutableArray<INamedTypeSymbol>.Empty;
			}

			var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

			while( containingType != null ) {

				builder.Add( containingType );
				containingType = containingType.ContainingType;
			}

			builder.Reverse();
			return builder.ToImmutable();
		}

	}
}
