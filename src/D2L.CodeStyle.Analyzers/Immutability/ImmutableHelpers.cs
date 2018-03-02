using System;
using System.Collections;
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
		/// Gets all of the [Immutable] attribute exceptions for this type, including inherited exceptions.
		/// </summary>
		public static ImmutableHashSet<string> GetAllImmutableExceptions( this ITypeSymbol type ) {

			ImmutableHashSet<string> directExceptions;
			if( type.TryGetDirectImmutableExceptions( out directExceptions ) ) {
				// If the type has direct exceptions, we can assume that the inherited exceptions are already
				// being properly analyzed for correctness, so we just return these.
				return directExceptions;
			}

			var inheritedImmutableExceptions = type.GetInheritedImmutableExceptions();

			if( inheritedImmutableExceptions.IsEmpty ) {
				throw new Exception( $"Tried to get all [Immutable] exceptions from a type ({type.GetFullTypeName()}) that was not directly marked [Immutable] and inherited from no [Immutable] types." );
			}

			var builder = ImmutableHashSet.CreateBuilder<string>();

			builder.UnionWith( inheritedImmutableExceptions.First().Value );

			foreach( var inheritedImmutableException in inheritedImmutableExceptions.Skip( 1 ) ) {
				builder.IntersectWith( inheritedImmutableException.Value );
			}

			return builder.ToImmutable();
		}

		/// <summary>
		/// Parses the Except values from a type's [Immutable] or [ImmutableBaseClass] attribute.
		/// </summary>
		public static bool TryGetDirectImmutableExceptions( this ITypeSymbol ty, out ImmutableHashSet<string> exceptions ) {

			// Externally owned immutable types have no annotation to pull exceptions from, but we want to treat them
			// as if they have no exceptions for the purposes of aggregating allowed Unaudited reasons.
			if( ty.IsExternallyOwnedMarkedImmutableType() ) {
				exceptions = ImmutableHashSet<string>.Empty;
				return true;
			}

			var immutableAttributes = ty.GetAllImmutableAttributesApplied().ToArray();
			if( immutableAttributes.Length == 0 ) {
				exceptions = null;
				return false;
			}

			var builder = ImmutableHashSet.CreateBuilder<string>();

			foreach( var immutableAttribute in immutableAttributes ) {
				var excepts = GetDirectImmutableException( immutableAttribute );
				builder.UnionWith( excepts );
			}

			exceptions = builder.ToImmutable();
			return true;
		}


		private static ImmutableHashSet<string> GetDirectImmutableException( AttributeData attrData ) {
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

			var set = ExceptFlagValuesToSet( flagsExpression );
			return set;
		}

		/// <summary>
		/// Gets all [Immutable] and [ImmutableBaseClass] attribute exceptions from this type's inherited types
		/// </summary>
		public static ImmutableDictionary<ISymbol, ImmutableHashSet<string>> GetInheritedImmutableExceptions( this ITypeSymbol type ) {

			var builder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableHashSet<string>>();

			ITypeSymbol baseType = type.BaseType;
			while( baseType != null ) {
				// Interfaces of the base types are covered in AllInterfaces below, so we only need to get direct exceptions
				// if they exist.
				ImmutableHashSet<string> baseTypeExceptions;
				if( baseType.TryGetDirectImmutableExceptions( out baseTypeExceptions ) ) {
					builder.Add( baseType, baseTypeExceptions );
				}
				baseType = baseType.BaseType;
			}

			foreach( INamedTypeSymbol iface in type.AllInterfaces ) {
				ImmutableHashSet<string> ifaceExceptions;
				if( iface.TryGetDirectImmutableExceptions( out ifaceExceptions ) ) {
					builder.Add( iface, ifaceExceptions );
				}
			}

			return builder.ToImmutable();
		}

		private static ImmutableHashSet<string> ExceptFlagValuesToSet( ExpressionSyntax expr ) {

			var builder = ImmutableHashSet.CreateBuilder<string>();

			ExceptFlagValuesToSetRecursive( expr, builder );

			return builder.ToImmutable();
		}

		private static void ExceptFlagValuesToSetRecursive( ExpressionSyntax expr, ImmutableHashSet<string>.Builder builder ) {

			if( expr is MemberAccessExpressionSyntax ) {

				var memEx = (MemberAccessExpressionSyntax)expr;

				string value = memEx.Name.Identifier.ValueText;
				// "None" values are skipped over. If there was only the one None value, the empty set will be returned as
				// expected. If there were other values, None is irrelevant since we only support OR operations.
				if( value != "None" ) {
					builder.Add( value );
				}
				return;
			}

			if( expr is BinaryExpressionSyntax ) {

				var binEx = (BinaryExpressionSyntax)expr;

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
