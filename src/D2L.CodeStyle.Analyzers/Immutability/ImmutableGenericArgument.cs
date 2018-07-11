using System;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	public static class ImmutableGenericArgument {
		public static bool InterfacesDemandImmutability(
			INamedTypeSymbol symbol,
			ITypeSymbol argument
		) {
			foreach( ITypeSymbol intf in symbol.Interfaces ) {
				if( SymbolDemandsImmutability(
					intf,
					argument
				) ) {
					return true;
				}
			}

			return false;
		}

		public static bool ConstraintsDemandImmutabliity(
			INamedTypeSymbol symbol,
			ITypeSymbol argument
		) {
			int argumentOrdinal = symbol.IndexOfArgument( argument.Name );
			if (argumentOrdinal < 0) {
				return false;
			}

			ITypeParameterSymbol argumentTypeParameter =
				symbol.TypeParameters[argumentOrdinal];

			foreach( ITypeSymbol constraint in argumentTypeParameter.ConstraintTypes ) {
				if( SymbolDemandsImmutability(
					constraint,
					argument
				) ) {
					return true;
				}
			}

			return false;
		}

		public static bool BaseClassDemandsImmutability(
			INamedTypeSymbol symbol,
			ITypeSymbol argument
		) {
			if( SymbolDemandsImmutability(
				symbol.BaseType,
				argument
			) ) {
				return true;
			}

			return false;
		}

		private static bool SymbolDemandsImmutability(
			ITypeSymbol symbol,
			ITypeSymbol argument
		) {
			var symbolType = symbol as INamedTypeSymbol;
			if( symbolType == default ) {
				return false;
			}

			int ordinal = symbolType.IndexOfArgument( argument.Name );
			if( ordinal < 0 ) {
				return false;
			}

			ImmutabilityScope argumentScope =
				symbolType.TypeParameters[ordinal].GetImmutabilityScope();

			if( argumentScope == ImmutabilityScope.SelfAndChildren ) {
				return true;
			}

			return false;
		}
	}
}
