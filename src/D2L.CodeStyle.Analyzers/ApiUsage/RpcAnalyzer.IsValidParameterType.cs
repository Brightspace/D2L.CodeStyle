using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {

	internal partial class RpcAnalyzer {

		private static bool IsValidParameterType(
			SyntaxNodeAnalysisContext context,
			ITypeSymbol type,
			INamedTypeSymbol deserializableType,
			INamedTypeSymbol deserializerType,
			INamedTypeSymbol dictionaryType,
			ImmutableHashSet<INamedTypeSymbol> knownRpcParameterTypes
		) {
			if( knownRpcParameterTypes.Contains( type ) ) {
				return true;
			}

			if( type.TypeKind == TypeKind.Enum ) {
				return true;
			}

			if( type.TypeKind == TypeKind.Array ) {
				var arrayType = ( type as IArrayTypeSymbol ).ElementType as INamedTypeSymbol;
				return IsValidParameterType(
					context: context,
					type: arrayType,
					deserializableType: deserializableType,
					deserializerType: deserializerType,
					dictionaryType: dictionaryType,
					knownRpcParameterTypes: knownRpcParameterTypes
				);
			}

			if( !( type is INamedTypeSymbol namedType ) ) {
				return false;
			}

			if( HasConstructorDeserializer( namedType, deserializerType ) ) {
				return true;
			}

			if( IsDeserializable( namedType, deserializableType ) ) {
				return true;
			}

			if( namedType.IsGenericType && SymbolEqualityComparer.Default.Equals( namedType.OriginalDefinition, dictionaryType ) ) {
				var dictionaryTypes = namedType.TypeArguments;

				if( !IsValidParameterType(
					context: context,
					type: dictionaryTypes[0],
					deserializableType: deserializableType,
					deserializerType: deserializerType,
					dictionaryType: dictionaryType,
					knownRpcParameterTypes: knownRpcParameterTypes
				) ) {
					return false;
				}

				if( !IsValidParameterType(
					context: context,
					type: dictionaryTypes[1],
					deserializableType: deserializableType,
					deserializerType: deserializerType,
					dictionaryType: dictionaryType,
					knownRpcParameterTypes: knownRpcParameterTypes
				) ) {
					return false;
				}

				return true;
			}

			return false;
		}

		private static bool HasConstructorDeserializer( INamedTypeSymbol type, INamedTypeSymbol deserializerType ) {
			if( deserializerType == null || deserializerType.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			foreach( var constructor in type.Constructors ) {
				var parameters = constructor.Parameters;

				if( parameters.Length != 1 ) {
					continue;
				}

				IParameterSymbol parameter = parameters[0];
				if( SymbolEqualityComparer.Default.Equals( parameter.Type, deserializerType ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsDeserializable( INamedTypeSymbol type, INamedTypeSymbol deserializableType ) {
			if( deserializableType == null || deserializableType.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			return type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, deserializableType ) );
		}
	}
}
