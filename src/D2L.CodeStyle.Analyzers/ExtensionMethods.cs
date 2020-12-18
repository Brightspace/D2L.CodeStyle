using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {

	internal static class ExtensionMethods {

		/// <summary>
		/// Check if the symbol has a specific attribute attached to it.
		/// </summary>
		/// <param name="symbol">The symbol to check for an attribute on</param>
		/// <param name="attributeClassName">The class of the attribute to check for</param>
		/// <returns>True if the attribute exists on the symbol, false otherwise</returns>
		public static bool HasAttribute(
			this ISymbol symbol,
			string attributeClassName
		) {
			// TODO: don't compare type names as strings
			bool hasExpectedAttribute = symbol.GetAttributes().Any(
				x => x.AttributeClass.GetFullTypeName() == attributeClassName
			);

			return hasExpectedAttribute;
		}
	}
}
