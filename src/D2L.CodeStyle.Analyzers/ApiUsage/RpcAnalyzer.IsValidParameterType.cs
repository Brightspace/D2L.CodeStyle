using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {

	internal partial class RpcAnalyzer {

		private static bool IsValidParameterType(
			SyntaxNodeAnalysisContext context,
			ITypeSymbol type,
			RpcTypes rpcTypes
		) {
			if( rpcTypes.KnownRpcParameters.Contains( type ) ) {
				return true;
			}

			if( type.TypeKind == TypeKind.Enum ) {
				return true;
			}

			if( type is IArrayTypeSymbol arrayType ) {
				return IsValidParameterType( context, arrayType.ElementType, rpcTypes );
			}

			if( !( type is INamedTypeSymbol namedType ) ) {
				return false;
			}

			if( HasConstructorDeserializer( namedType, rpcTypes ) ) {
				return true;
			}

			if( IsDeserializable( namedType, rpcTypes ) ) {
				return true;
			}

			if( namedType.IsGenericType && SymbolEqualityComparer.Default.Equals( namedType.OriginalDefinition, rpcTypes.IDictionary ) ) {
				var dictionaryTypes = namedType.TypeArguments;

				if( !IsValidParameterType( context, dictionaryTypes[0], rpcTypes ) ) {
					return false;
				}

				if( !IsValidParameterType( context, dictionaryTypes[1], rpcTypes ) ) {
					return false;
				}

				return true;
			}

			return false;
		}

		private static bool HasConstructorDeserializer( INamedTypeSymbol type, RpcTypes rpcTypes ) {
			if( rpcTypes.IDeserializer == null || rpcTypes.IDeserializer.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			foreach( var constructor in type.Constructors ) {
				if( constructor.DeclaredAccessibility != Accessibility.Public ) {
					continue;
				}

				var parameters = constructor.Parameters;

				if( parameters.Length != 1 ) {
					continue;
				}

				IParameterSymbol parameter = parameters[0];
				if( SymbolEqualityComparer.Default.Equals( parameter.Type, rpcTypes.IDeserializer ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsDeserializable( INamedTypeSymbol type, RpcTypes rpcTypes ) {
			if( rpcTypes.IDeserializable == null || rpcTypes.IDeserializable.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			return type.AllInterfaces.Any( i => SymbolEqualityComparer.Default.Equals( i, rpcTypes.IDeserializable ) );
		}
	}
}
