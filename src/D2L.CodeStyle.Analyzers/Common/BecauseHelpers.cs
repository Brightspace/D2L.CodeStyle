using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

			string rawReason = unauditedAttr
				.ConstructorArguments
				.FirstOrDefault()
				.ToCSharpString()
				.Split( '.' )
				.Last();

			return Enum.TryParse( rawReason, out reason );
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

			ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs = attrData.NamedArguments;

			if( !namedArgs.Any( kvp => kvp.Key == "Except" ) ) {
				return DefaultImmutabilityExceptions;
			}

			string rawAllowedReasons = namedArgs
				.First( kvp => kvp.Key == "Except" )
				.Value
				.ToCSharpString();

			List<string> enumParseableReasons = rawAllowedReasons
				.Split( '|' )
				.Select( r => r.Trim().Split( '.' ).Last() )
				.ToList();

			if( enumParseableReasons.Count == 1 && enumParseableReasons[0] == "None" ) {
				return ImmutableHashSet<Because>.Empty;
			}

			ImmutableHashSet<Because>.Builder exceptions = ImmutableHashSet.CreateBuilder<Because>();
			foreach( var reason in enumParseableReasons ) {
				Because cuz;
				if( Enum.TryParse( reason, out cuz ) ) {
					exceptions.Add( cuz );
				}
			}
			return exceptions.ToImmutable();
		}

	}
}
