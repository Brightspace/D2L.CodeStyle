using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common {
	internal static class BecauseHelpers {

		public static bool TryGetUnauditedReason( ISymbol symbol, out string reason ) {

			// If it's an error, don't bother parsing anything more
			if( symbol is IErrorTypeSymbol ) {
				reason = null;
				return false;
			}

			SyntaxNode syntaxNode = Attributes.Mutability.Unaudited
				.GetAll( symbol )
				.FirstOrDefault()?
				.ApplicationSyntaxReference?
				.GetSyntax();

			AttributeSyntax attrSyntax = syntaxNode as AttributeSyntax;

			if( attrSyntax == null ) {
				reason = null;
				return false;
			}

			AttributeArgumentSyntax foundArg = attrSyntax
				.ArgumentList
				.Arguments
				.FirstOrDefault( arg => {
					// Get the first argument that is not defined by a "Name = ..." syntax and either is not named or is named "why"
					return arg.NameEquals == null && ( arg.NameColon == null || arg.NameColon.Name.Identifier.ValueText == "why" );
				} );

			if( foundArg == null ) {
				reason = null;
				return false;
			}

			MemberAccessExpressionSyntax expr = foundArg.Expression as MemberAccessExpressionSyntax;
			if( expr == null ) {
				reason = null;
				return false;
			}

			reason = expr.Name.Identifier.ValueText;
			return true;
		}

	}
}
