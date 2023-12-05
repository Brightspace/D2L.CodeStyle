using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {
	public static class DeserializableAnalyzerHelper {

		public const string MustBeDeserializableAttributeName = "D2L.CodeStyle.Annotations.Serialization.MustBeDeserializableAttribute";
		public const string ReflectionSerializerAttributeName = "D2L.LP.Serialization.ReflectionSerializerAttribute";
		public const string SerializerAttributeName = "D2L.LP.Serialization.SerializerAttribute";

		internal static DeserializableTypeInfo? GetDeserializableTypeInfo( Compilation compilation) {
			INamedTypeSymbol? recursiveSymbol = compilation.GetTypeByMetadataName( MustBeDeserializableAttributeName );

			if( recursiveSymbol == null ) {
				return null;
			}

			List<INamedTypeSymbol> validAttributes = new List<INamedTypeSymbol>();
			INamedTypeSymbol? reflectionSerializerSymbol = compilation.GetTypeByMetadataName( ReflectionSerializerAttributeName );
			if( reflectionSerializerSymbol == null ) {
				return null;
			}

			validAttributes.Add( reflectionSerializerSymbol );

			INamedTypeSymbol? serializerSymbol = compilation.GetTypeByMetadataName( SerializerAttributeName );
			if( serializerSymbol != null ) {
				validAttributes.Add( serializerSymbol );
			}

			return new DeserializableTypeInfo( recursiveSymbol, Diagnostics.MustBeDeserializableRequiresAppropriateAttribute, Diagnostics.ArgumentShouldBeDeserializable, validAttributes.ToArray() );
		}

		internal static bool HasMustBeDeserializableAttribute( ISymbol symbol, DeserializableTypeInfo deserializableTypeInfo ) {
			var attributes = symbol.GetAttributes();
			foreach( var attributeData in attributes ) {
				var attributeSymbol = attributeData.AttributeClass;
				if( deserializableTypeInfo.MustBeDeserializableAttribute.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) {
					return true;
				}
			}

			return false;
		}

		internal static bool IsDeserializable( ISymbol classSymbol, DeserializableTypeInfo deserializableTypeInfo ) {
			foreach( var attributeData in classSymbol.GetAttributes() ) {
				var attributeSymbol = attributeData.AttributeClass;

				if( deserializableTypeInfo.ValidAttributes.Any( a => a.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) ) {
					return true;
				}
			}

			return false;
		}

		internal static bool IsDeserializableAtAllLevels ( ITypeSymbol classSymbol, Func<ITypeSymbol, bool> inAllowedList, DeserializableTypeInfo deserializableTypeInfo ) {

			if( !IsExemptFromNeedingSerializationAttributes( classSymbol, inAllowedList, out ITypeSymbol actualType)
				&& !IsDeserializable(actualType, deserializableTypeInfo)) {
				return false;
			}

			if( classSymbol is INamedTypeSymbol namedTypeSymbol ) {
				foreach( ITypeSymbol childType in namedTypeSymbol.TypeArguments ) {
					if( !IsDeserializableAtAllLevels( childType, inAllowedList, deserializableTypeInfo ) ) {
						return false;
					}
				}
			}

			return true;
		}

		public static Func<ITypeSymbol, bool> GetAllowListFunction( ImmutableArray<AdditionalText> additionalFiles, Compilation compilation ) {
			List<ISymbol>? allowNonSerializable = null;
			var allowed = ( ITypeSymbol symbol ) => {
				if( symbol.SpecialType != SpecialType.None && symbol.SpecialType != SpecialType.System_Object ) {
					return true;
				}
				if( allowNonSerializable == null ) {
					allowNonSerializable = new List<ISymbol>();
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
							allowNonSerializable.Add( type );
						}
					}
				}

				var match = allowNonSerializable.FirstOrDefault( u => u.Equals( symbol, SymbolEqualityComparer.Default ) );

				if( match == null ) {
					match = allowNonSerializable.FirstOrDefault( u => u.Equals( symbol.OriginalDefinition, SymbolEqualityComparer.Default ) );
				}

				return match != null;
			};
			return allowed;
		}

		public static bool IsExemptFromNeedingSerializationAttributes( ITypeSymbol typeSymbol, Func<ITypeSymbol, bool> inAllowList, out ITypeSymbol actualType ) {
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

			if( inAllowList( typeSymbol ) || inAllowList( currentType ) ) {
				return true;
			}

			return false;
		}

		private static ITypeSymbol? GetEnumerableTypeArgument( ITypeSymbol typeSymbol ) {

			// Check if the type is an array. If so, return the element type.
			if( typeSymbol.Kind == SymbolKind.ArrayType ) {
				return ( (IArrayTypeSymbol)typeSymbol ).ElementType;
			}

			const string iEnumerableId = "System.Collections.Generic.IEnumerable<T>";
			// Check if the type is named type symbol. It must be in order to be IEnumerable<T>
			if( typeSymbol is INamedTypeSymbol namedTypeSymbol && !typeSymbol.Locations.Any( l => l.IsInSource ) ) {
				// If it's directly IEnumerable<T>
				if( namedTypeSymbol.ConstructedFrom.ToDisplayString() == iEnumerableId ) {
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
