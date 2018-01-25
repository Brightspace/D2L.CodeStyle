using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal static class BecauseHelpers {

		public static string GetUnauditedReason( ISymbol symbol ) {

			AttributeData attrData = Attributes.Mutability.Unaudited
				.GetAll( symbol )
				.FirstOrDefault();

			if( attrData == null ) {
				throw new Exception( $"Unable to get Unaudited attribute on '{symbol.Name}'" );
			}

			SyntaxNode syntaxNode = attrData
				.ApplicationSyntaxReference?
				.GetSyntax();

			AttributeSyntax attrSyntax = syntaxNode as AttributeSyntax;

			if( attrSyntax == null ) {
				throw new Exception( $"Unable to get AttributeSyntax for Unaudited attribute on '{symbol.Name}'" );
			}

			AttributeArgumentSyntax foundArg = attrSyntax
				.ArgumentList?
				.Arguments
				.FirstOrDefault(
					// Get the first argument that is not defined by a "Name = ..." syntax and either is not named or is named "why"
					arg => arg.NameEquals == null && ( arg.NameColon == null || arg.NameColon.Name.Identifier.ValueText == "why" )
				);

			if( foundArg == null ) {
				throw new Exception( $"Could not find Unaudited argument for Because reason in '{attrSyntax}'" );
			}

			MemberAccessExpressionSyntax expr = foundArg.Expression as MemberAccessExpressionSyntax;
			if( expr == null ) {
				throw new Exception( $"Unaudited argument for Because reason was not a MemberAccessExpression in '{attrSyntax}'" );
			}

			string reasonName = expr.Name?.Identifier.ValueText;

			if( reasonName == null ) {
				throw new Exception( $"Unable to get Because variant name in '{attrSyntax}'" );
			}

			return reasonName;
		}

	}
}
