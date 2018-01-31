using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal static class ImmutableHelpers {

		/// <summary>
		/// These values are used as the exceptions when a type is marked [Immutable] with no defined exceptions.
		/// </summary>
		public static readonly ImmutableHashSet<string> DefaultImmutabilityExceptions = ImmutableHashSet.Create(
			"ItHasntBeenLookedAt",
			"ItsSketchy",
			"ItsStickyDataOhNooo",
			"WeNeedToMakeTheAnalyzerConsiderThisSafe",
			"ItsUgly",
			"ItsOnDeathRow"
		);

		/// <summary>
		/// Parses the Except values from a type's [Immutable] attribute.
		/// </summary>
		public static ImmutableHashSet<string> GetActualImmutableExceptions( this ITypeSymbol ty ) {

			AttributeData attrData = Attributes.Objects.Immutable.GetAll( ty ).FirstOrDefault();
			if( attrData == null ) {
				throw new Exception( $"Unable to get Immutable attribute from type '{ty.GetFullTypeName()}'" );
			}

			SyntaxNode syntaxNode = attrData
				.ApplicationSyntaxReference?
				.GetSyntax();

			AttributeSyntax attrSyntax = syntaxNode as AttributeSyntax;

			ExpressionSyntax flagsExpression;

			// If we can't get the attribute syntax but we know the Immutable attribute exists, we're analyzing a type
			// from another assembly. We can analyze it by parsing its value back into syntax.
			if( attrSyntax == null ) {

				TypedConstant exceptValue = attrData
					.NamedArguments
					.FirstOrDefault( kvp => kvp.Key == "Except" )
					.Value;

				flagsExpression = exceptValue.IsNull
					? null
					: SyntaxFactory.ParseExpression( exceptValue.ToCSharpString() );

			} else {

				AttributeArgumentSyntax foundArg = attrSyntax
					.ArgumentList?
					.Arguments
					.FirstOrDefault(
						// Get the first argument that is defined by the "Except = ..." syntax
						arg => arg.NameEquals?.Name?.Identifier.ValueText == "Except"
					);

				flagsExpression = foundArg?.Expression;
			}

			if( flagsExpression == null ) {
				// We have the Immutable attribute but no Except value, so we just return the defaults
				return DefaultImmutabilityExceptions;
			}

			ImmutableHashSet<string> exceptions = ExceptFlagValuesToSet( flagsExpression );
			return exceptions;
		}

		private static ImmutableHashSet<string> ExceptFlagValuesToSet( ExpressionSyntax expr ) {

			ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder<string>();

			ExceptFlagValuesToSetRecursive( expr, builder );

			return builder.ToImmutable();
		}

		private static void ExceptFlagValuesToSetRecursive( ExpressionSyntax expr, ImmutableHashSet<string>.Builder builder ) {

			if( expr is MemberAccessExpressionSyntax ) {
				MemberAccessExpressionSyntax memEx = (MemberAccessExpressionSyntax)expr;
				string value = memEx.Name.Identifier.ValueText;
				// "None" values are skipped over. If there was only the one None value, the empty set will be returned as
				// expected. If there were other values, None is irrelevant since we only support OR operations.
				if( value != "None" ) {
					builder.Add( value );
				}
				return;
			}

			if( expr is BinaryExpressionSyntax ) {
				BinaryExpressionSyntax binEx = (BinaryExpressionSyntax)expr;

				if( binEx.Kind() != SyntaxKind.BitwiseOrExpression ) {
					throw new Exception( $"Flags are being created with unsupported operator: '{binEx}" );
				}

				ExceptFlagValuesToSetRecursive( binEx.Right, builder );
				ExceptFlagValuesToSetRecursive( binEx.Left, builder );
				return;
			}

			throw new Exception( $"Unknown expression syntax type '{expr.GetType()}' when parsing flags: '{expr}'" );
		}

	}
}
