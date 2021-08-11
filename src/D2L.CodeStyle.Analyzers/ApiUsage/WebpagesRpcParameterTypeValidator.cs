using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {

	internal sealed class WebpagesRpcParameterTypeValidator {

		public static bool IsValidParameterType(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol type,
			INamedTypeSymbol deserializerType,
			ImmutableHashSet<INamedTypeSymbol> knownRpcParameterTypes
		) {

			bool isDeserializerTypeDefined = deserializerType != null && deserializerType.Kind != SymbolKind.ErrorType;

			if( knownRpcParameterTypes.Contains( type ) ) {
				return true;
			}

			if( type.EnumUnderlyingType != null ) {
				return true;
			}

			if( isDeserializerTypeDefined && HasConstructorDeserializer( type, deserializerType ) ) {
				return true;
			}

			if( isDeserializerTypeDefined && IsDeserializable( type, deserializerType ) ) {
				return true;
			}

			if( type.TypeKind == TypeKind.Array ) {
				var arrayType = ( type as IArrayTypeSymbol ).ElementType as INamedTypeSymbol;
				if( IsValidParameterType( context, arrayType, deserializerType, knownRpcParameterTypes ) ) {
					return true;
				}
			}

			if( type.IsGenericType && SymbolEqualityComparer.Default.Equals( type.OriginalDefinition, context.Compilation.GetTypeByMetadataName( "System.Collections.Generic.IDictionary<,>" ) ) ) {
				var dictionaryTypes = type.TypeArguments;
				bool validKeyType = IsValidParameterType( context, dictionaryTypes[0] as INamedTypeSymbol, deserializerType, knownRpcParameterTypes );
				bool validValueType = IsValidParameterType( context, dictionaryTypes[1] as INamedTypeSymbol, deserializerType, knownRpcParameterTypes );

				if( validKeyType && validValueType ) {
					return true;
				}
			}

			return false;
		}

		private static bool HasConstructorDeserializer( INamedTypeSymbol type, INamedTypeSymbol deserializerType ) {
			var deserializer = type.Constructors
				.Where( constructorSymbol => constructorSymbol.TypeArguments.Contains( deserializerType ) );
			return deserializer.Any();
		}

		private static bool IsDeserializable( INamedTypeSymbol type, INamedTypeSymbol deserializerType ) {
			return deserializerType.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, type ) );
		}
	}
}
