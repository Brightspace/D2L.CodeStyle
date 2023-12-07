using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {
	public static class DeserializableAnalyzerHelper {

		private const string MustBeDeserializableAttributeName = "D2L.CodeStyle.Annotations.Serialization.MustBeDeserializableAttribute";
		private const string ReflectionSerializerAttributeName = "D2L.LP.Serialization.ReflectionSerializerAttribute";
		private const string SerializerAttributeName = "D2L.LP.Serialization.SerializerAttribute";

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

			INamedTypeSymbol? serializableSymbol = compilation.GetTypeByMetadataName( "System.SerializableAttribute" );
			if( serializableSymbol != null ) {
				validAttributes.Add( serializableSymbol );
			}

			return new DeserializableTypeInfo( recursiveSymbol, Diagnostics.MustBeDeserializableRequiresAppropriateAttribute, Diagnostics.ArgumentShouldBeDeserializable, validAttributes.ToArray() );
		}

		internal static bool HasMustBeDeserializableAttribute( ISymbol symbol, DeserializableTypeInfo deserializableTypeInfo ) {
			ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
			foreach( AttributeData? attributeData in attributes ) {
				INamedTypeSymbol? attributeSymbol = attributeData.AttributeClass;
				if( deserializableTypeInfo.MustBeDeserializableAttribute.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) {
					return true;
				}
			}

			return false;
		}

		internal static bool HasReflectionSerializerAttribe( ISymbol symbol, DeserializableTypeInfo deserializableTypeInfo ) {
			foreach( AttributeData? attributeData in symbol.GetAttributes() ) {
				INamedTypeSymbol? attributeSymbol = attributeData.AttributeClass;

				if( deserializableTypeInfo.ValidAttributes.Any( a => a.Name =="ReflectionSerializerAttribute" && a.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) ) {
					return true;
				}
			}

			return false;
		}

		internal static bool IsDeserializable( ITypeSymbol classSymbol, DeserializableTypeInfo deserializableTypeInfo ) {
			classSymbol = GetActualSymbol( classSymbol );
			foreach( AttributeData? attributeData in classSymbol.GetAttributes() ) {
				INamedTypeSymbol? attributeSymbol = attributeData.AttributeClass;

				if( deserializableTypeInfo.ValidAttributes.Any( a => a.Equals( attributeSymbol, SymbolEqualityComparer.Default ) ) ) {
					return true;
				}
			}

			return false;
		}

		internal static bool IsDeserializableAtAllLevels(ITypeSymbol classSymbol, Func<ITypeSymbol, bool> inAllowedList, DeserializableTypeInfo deserializableTypeInfo)
        {
            Queue<ITypeSymbol> typeQueue = new Queue<ITypeSymbol>();
            typeQueue.Enqueue(classSymbol);

            while (typeQueue.Count > 0)
            {
                ITypeSymbol currentType = typeQueue.Dequeue();

                if (!IsExemptFromNeedingSerializationAttributes(currentType, inAllowedList, out ITypeSymbol actualType)
                    && !IsDeserializable(actualType, deserializableTypeInfo))
                {
                    return false;
                }

                if (currentType is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (ITypeSymbol childType in namedTypeSymbol.TypeArguments)
                    {
                        typeQueue.Enqueue(childType);
                    }
                }
            }

            return true;
        }

		public static Func<ITypeSymbol, bool> GetAllowListFunction( ImmutableArray<AdditionalText> additionalFiles, Compilation compilation ) {
			List<ISymbol>? allowNonSerializable = null;

			bool Allowed( ITypeSymbol symbol ) {
				if( symbol.SpecialType != SpecialType.None && symbol.SpecialType != SpecialType.System_Object ) {
					return true;
				}

				if( allowNonSerializable == null ) {
					allowNonSerializable = new List<ISymbol>();
					AdditionalText? file = additionalFiles.FirstOrDefault( f => f.Path.EndsWith( "UnpinnedAllowedList.txt" ) );
					if( file == null ) {
						return false;
					}

					SourceText? text = file.GetText();
					if( text == null ) {
						return false;
					}

					foreach( TextLine line in text.Lines ) {
						INamedTypeSymbol? type = compilation.GetTypeByMetadataName( line.ToString().Trim() );
						if( type != null ) {
							allowNonSerializable.Add( type );
						}
					}
				}

				ISymbol? match = allowNonSerializable.FirstOrDefault( u => u.Equals( symbol, SymbolEqualityComparer.Default ) );

				if( match == null ) {
					match = allowNonSerializable.FirstOrDefault( u => u.Equals( symbol.OriginalDefinition, SymbolEqualityComparer.Default ) );
				}

				return match != null;
			}

			return Allowed;
		}

		public static Func<ITypeSymbol, bool> IsDeserializableWithoutSerializerAttribute(ImmutableArray<AdditionalText> additionalFiles, Compilation compilation  ) {
			Func<ITypeSymbol, bool> inAllowedList = DeserializableAnalyzerHelper.GetAllowListFunction( additionalFiles, compilation );

			return (ITypeSymbol symbol) => DeserializableAnalyzerHelper.IsExemptFromNeedingSerializationAttributes( symbol, inAllowedList, out _ );
		}


		public static bool IsExemptFromNeedingSerializationAttributes( ITypeSymbol typeSymbol, Func<ITypeSymbol, bool> inAllowList, out ITypeSymbol actualType ) {
			ITypeSymbol currentType = GetActualSymbol( typeSymbol );

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

		private static ITypeSymbol GetActualSymbol( ITypeSymbol typeSymbol ) {
			ITypeSymbol currentType = typeSymbol;


			if( currentType.TypeKind == TypeKind.Array && currentType is IArrayTypeSymbol arrayTypeSymbol ) {
				currentType = arrayTypeSymbol.ElementType;
			}

			if( currentType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T ) {
				return currentType;
			}

			// Get the non-nullable type argument
			ITypeSymbol? nonNullableType = ( currentType as INamedTypeSymbol )?.TypeArguments[0];
			if( nonNullableType != null ) {
				currentType = nonNullableType;
			}

			return currentType;
		}
	}
}
