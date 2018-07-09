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
					intf as INamedTypeSymbol,
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
			ITypeParameterSymbol argumentTypeParameter =
				symbol.TypeParameters[argumentOrdinal];

			if( argumentTypeParameter == default ) {
				// This can't happen for generic types, but just to be sure
				// we'll fail-safe rather than exploding
				return true;
			}

			foreach( ITypeSymbol constraint in argumentTypeParameter.ConstraintTypes ) {
				if( SymbolDemandsImmutability(
					constraint as INamedTypeSymbol,
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
				symbol.BaseType as INamedTypeSymbol,
				argument
			) ) {
				return true;
			}

			return false;
		}

		private static bool SymbolDemandsImmutability(
			INamedTypeSymbol symbol,
			ITypeSymbol argument
		) {
			if( symbol == default ) {
				return false;
			}

			int ordinal = symbol.IndexOfArgument( argument.Name );
			if( ordinal < 0 ) {
				return false;
			}

			ImmutabilityScope argumentScope =
				symbol.TypeParameters[ordinal].GetImmutabilityScope();

			if( argumentScope == ImmutabilityScope.SelfAndChildren ) {
				return true;
			}

			return false;
		}
	}
}
