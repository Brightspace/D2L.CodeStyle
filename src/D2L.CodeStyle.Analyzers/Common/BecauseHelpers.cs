using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common {
	internal static class BecauseHelpers {

		private static readonly ImmutableHashSet<Because> DefaultImmutabilityExceptions
			= Enum.GetValues( typeof( Because ) ).Cast<Because>().ToImmutableHashSet();

		/// <summary>
		/// Gets the Because value from a field or property annotated with Mutability.Unaudited
		/// </summary>
		/// <param name="symbol">The symbol to get the reason for</param>
		/// <param name="reason">The reason, if it was annotated with a valid value</param>
		/// <returns>true if it was annotated with a valid value; false otherwise</returns>
		public static bool TryGetUnauditedReason( ISymbol symbol, out Because reason ) {

			AttributeData unauditedAttr = Attributes.Mutability.Unaudited.GetAll( symbol ).FirstOrDefault();

			if( unauditedAttr == null ) {
				reason = default( Because );
				return false;
			}

			int rawReason = (int)unauditedAttr
				.ConstructorArguments
				.FirstOrDefault()
				.Value;

			return TryParseUnauditedReason( rawReason, out reason );
		}

		private static bool TryParseUnauditedReason( int value, out Because reason ) {
			if( !Enum.IsDefined( typeof( Because ), value ) ) {
				reason = default( Because );
				return false;
			}
			reason = (Because)value;
			return true;
		}

		/// <summary>
		/// Gets the exceptions to the Immutable analyzer from a type annotated with the Immutable attribute
		/// </summary>
		/// <param name="symbol">The symbol of the type to get the exceptions for</param>
		/// <returns>
		/// <see cref="DefaultImmutabilityExceptions"/> if the type was not immutable or specified no exceptions.
		/// Otherwise the parsed set of exceptions.
		/// </returns>
		public static IImmutableSet<Because> GetImmutabilityExceptions( ITypeSymbol symbol ) {

			AttributeData attrData;
			if( !symbol.TryGetImmutableAttributeData( out attrData ) ) {
				return DefaultImmutabilityExceptions;
			}

			TypedConstant allowedReasonsConst = attrData
				.NamedArguments
				.FirstOrDefault( kvp => kvp.Key == "Except" )
				.Value;

			if( allowedReasonsConst.Value == null ) {
				return DefaultImmutabilityExceptions;
			}

			int allowedReasons = (int)allowedReasonsConst.Value;
			return ParseImmutabilityExceptions( allowedReasons );
		}

		private static IImmutableSet<Because> ParseImmutabilityExceptions( int value ) {
			// These values must always be kept in sync with the values in Objects.Except
			ImmutableHashSet<Because>.Builder builder = ImmutableHashSet.CreateBuilder<Because>();
			if( value >= 32 ) {
				builder.Add( Because.ItsOnDeathRow );
				value -= 32;
			}
			if( value >= 16 ) {
				builder.Add( Because.ItsUgly );
				value -= 16;
			}
			if( value >= 8 ) {
				builder.Add( Because.WeNeedToMakeTheAnalyzerConsiderThisSafe );
				value -= 8;
			}
			if( value >= 4 ) {
				builder.Add( Because.ItsStickyDataOhNooo );
				value -= 4;
			}
			if( value >= 2 ) {
				builder.Add( Because.ItsSketchy );
				value -= 2;
			}
			if( value >= 1 ) {
				builder.Add( Because.ItHasntBeenLookedAt );
				value -= 1;
			}
			if( value != 0 ) {
				throw new Exception( "Unknown variant in allowed unaudited reasons" );
			}
			return builder.ToImmutable();
		}

	}
}
