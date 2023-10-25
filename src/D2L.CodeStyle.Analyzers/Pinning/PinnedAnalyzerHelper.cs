﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Pinning {
	public static class PinnedAnalyzerHelper {

		public const string PinnedAttributeName = "D2L.CodeStyle.Annotations.Pinning.PinnedAttribute";
		public const string MustBeDeserializableAttributeName = "D2L.CodeStyle.Annotations.Pinning.MustBeDeserializableAttribute";
		public const string MustBePinnedAttributeName = "D2L.CodeStyle.Annotations.Pinning.MustBePinnedAttribute";

		public static bool TryGetPinnedAttribute( ISymbol classSymbol, INamedTypeSymbol pinnedAttributeSymbol, out AttributeData? attribute ) {
			attribute = null;

			foreach( var attributeData in classSymbol.GetAttributes() ) {
				var attributeSymbol = attributeData.AttributeClass;
				if( pinnedAttributeSymbol.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) {
					attribute = attributeData;
					return true;
				}
			}

			return false;
		}

		internal static bool HasAppropriateMustBePinnedAttribute( ISymbol symbol, MustBePinnedType pinningType, out AttributeData? attribute ) {
			attribute = null;
			var attributes = symbol.GetAttributes();
			foreach( var attributeData in  attributes) {
				var attributeSymbol = attributeData.AttributeClass;
				if( pinningType.PinnedAttributeSymbol.Equals( attributeSymbol, SymbolEqualityComparer.Default )
					|| ( pinningType.AlternatePinnedAttribute != null && pinningType.AlternatePinnedAttribute.Equals(attributeSymbol, SymbolEqualityComparer.Default))) {
					attribute = attributeData;
					return true;
				}
			}

			return false;
		}

		public static bool IsRecursivelyPinned( ISymbol classSymbol, INamedTypeSymbol pinnedAttributeSymbol ) {
			if( !TryGetPinnedAttribute( classSymbol, pinnedAttributeSymbol, out AttributeData? attributeData ) ) {
				return false;
			}
			object parameterValue = attributeData!.ConstructorArguments[2].Value!;

			return (bool)parameterValue;
		}

		public static Func<ITypeSymbol, bool> AllowedUnpinnedTypes(ImmutableArray<AdditionalText> additionalFiles, Compilation compilation) {
			List<ISymbol>? allowUnpinned = null;
			var canBeUnpinned = ( ITypeSymbol symbol ) => {
				if( symbol.SpecialType != SpecialType.None && symbol.SpecialType != SpecialType.System_Object ) {
					return true;
				}
				if( allowUnpinned == null ) {
					allowUnpinned = new List<ISymbol>();
					var file = additionalFiles.FirstOrDefault( f => f.Path.EndsWith( "UnpinnedAllowedList.txt" ) );
					if( file == null ) {
						return false;
					}

					var text = file.GetText();
					if( text == null ) {
						return false;
					}
					foreach( var line in text.Lines ) {
						var type = compilation.GetTypeByMetadataName( line.ToString().Trim() );
						if( type != null ) {
							allowUnpinned.Add( type );
						}
					}
				}

				var match = allowUnpinned.FirstOrDefault( u => u.Equals( symbol, SymbolEqualityComparer.Default ) );

				if( match == null ) {
					match = allowUnpinned.FirstOrDefault( u => u.Equals( symbol.OriginalDefinition, SymbolEqualityComparer.Default ) );
				}

				return match != null;
			};
			return canBeUnpinned;
		}

		public static bool IsExemptFromPinning(ITypeSymbol typeSymbol, Func<ITypeSymbol, bool> inAllowList, out ITypeSymbol actualType ) {
			var currentType = typeSymbol;
			var enumerableTypeArgument = GetEnumerableTypeArgument( currentType );
			while( enumerableTypeArgument != null ) {
				currentType = enumerableTypeArgument;
				enumerableTypeArgument = GetEnumerableTypeArgument( currentType );
			}

			if( currentType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ) {
				// Get the non-nullable type argument
				ITypeSymbol? nonNullableType = ( currentType as INamedTypeSymbol )?.TypeArguments[0];
				if( nonNullableType != null ) {
					currentType = nonNullableType;
				}
			}

			actualType = currentType;

			// Don't analyze primitive types
			if( currentType.SpecialType != SpecialType.None && currentType.SpecialType != SpecialType.System_Object ) {
				return true;
			}

			if( currentType.TypeKind == TypeKind.Enum ) {
				return true;
			}

			if(inAllowList(typeSymbol) || inAllowList(currentType)) {
				return true;
			}

			return false;
		}

		private static ITypeSymbol? GetEnumerableTypeArgument( ITypeSymbol typeSymbol) {

			// Check if the type is an array. If so, return the element type.
			if( typeSymbol.Kind == SymbolKind.ArrayType ) {
				return ( (IArrayTypeSymbol)typeSymbol ).ElementType;
			}

			const string iEnumerableId = "System.Collections.Generic.IEnumerable<T>";
			// Check if the type is named type symbol. It must be in order to be IEnumerable<T>
			if( typeSymbol is INamedTypeSymbol namedTypeSymbol && !typeSymbol.Locations.Any( l => l.IsInSource ) ) {
				// If it's directly IEnumerable<T>
				if( namedTypeSymbol.ConstructedFrom.ToDisplayString() == iEnumerableId  ) {
					return namedTypeSymbol.TypeArguments[0];
				}

				// If implements IEnumerable<T>
				var match = namedTypeSymbol.Interfaces.FirstOrDefault( i => i.ConstructedFrom.ToDisplayString() == iEnumerableId );
				if( match != null ) {
					return match.TypeArguments[0];
				}
			}

			// It's not an IEnumerable<T>
			return null;
		}
	}
}
