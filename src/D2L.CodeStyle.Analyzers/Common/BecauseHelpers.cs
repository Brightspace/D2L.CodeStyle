using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace D2L.CodeStyle.Analyzers.Common {
	internal static class BecauseHelpers {

		public static readonly IImmutableSet<string> DefaultImmutabilityExceptions = ImmutableHashSet.Create(
				"ItHasntBeenLookedAt",
				"ItsSketchy",
				"ItsStickyDataOhNooo",
				"WeNeedToMakeTheAnalyzerConsiderThisSafe",
				"ItsUgly",
				"ItsOnDeathRow"
			);

		/// <summary>
		/// Gets the Because value from a field or property annotated with Mutability.Unaudited
		/// </summary>
		/// <param name="symbol">The symbol to get the reason for</param>
		/// <param name="reason">The reason, if it was annotated with a valid value</param>
		/// <returns>true if it was annotated with a valid value; false otherwise</returns>
		public static bool TryGetUnauditedReason( ISymbol symbol, out string reason ) {

			AttributeData unauditedAttr = Attributes.Mutability.Unaudited.GetAll( symbol ).FirstOrDefault();

			if( unauditedAttr == null ) {
				reason = string.Empty;
				return false;
			}

			reason = unauditedAttr
				.ConstructorArguments
				.FirstOrDefault()
				.ToCSharpString()
				.Split( '.' )
				.Last();

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
		public static IImmutableSet<string> GetImmutabilityExceptions( ITypeSymbol symbol ) {

			AttributeData attrData;
			if( !symbol.TryGetImmutableAttributeData( out attrData ) ) {
				return DefaultImmutabilityExceptions;
			}

			ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs = attrData.NamedArguments;

			if( !namedArgs.Any( kvp => kvp.Key == "Except" ) ) {
				return DefaultImmutabilityExceptions;
			}

			List<string> reasons = namedArgs
				.First( kvp => kvp.Key == "Except" )
				.Value
				.ToCSharpString()
				.Split( '|' )
				.Select( r => r.Trim().Split( '.' ).Last() )
				.ToList();

			ImmutableHashSet<string>.Builder exceptions = ImmutableHashSet.CreateBuilder<string>();
			foreach( var reason in reasons ) {
				if( reason == "None" ) {
					continue;
				}
				exceptions.Add( reason );
			}
			return exceptions.ToImmutable();
		}

	}
}
