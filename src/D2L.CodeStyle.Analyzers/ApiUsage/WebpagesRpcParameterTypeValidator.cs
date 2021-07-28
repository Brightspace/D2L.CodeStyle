using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.ApiUsage {

	internal sealed class WebpagesRpcParameterTypeValidator {

		private static readonly ImmutableHashSet<Type> KnownRpcParameterTypes = ImmutableHashSet.Create<Type>(
			typeof( bool ),
			typeof( decimal ),
			typeof( double ),
			typeof( float ),
			typeof( int ),
			typeof( long ),
			typeof( string ),
			typeof( IDictionary<string, string> )
		);

		public static bool IsValidParameterType( Type type, Type deserializerType ) {

			if( KnownRpcParameterTypes.Contains( type ) ) {
				return true;
			}

			if( type.Name == "ITreeNode" ) {
				return true;
			}

			if( type.IsEnum ) {
				return true;
			}

			if( HasConstructorDeserializer( type, deserializerType ) ) {
				return true;
			}

			if( IsDeserializable( type, deserializerType ) ) {
				return true;
			}

			if( type.IsArray ) {
				if( IsValidParameterType( type.GetElementType(), deserializerType ) ) {
					return true;
				}
			}

			if( type.IsGenericType && type.GetGenericTypeDefinition().Equals( typeof( IDictionary<,> ) ) ) {
				Type[] dictionaryTypes = type.GetGenericArguments();
				bool validKeyType = IsValidParameterType( dictionaryTypes[0], deserializerType );
				bool validValueType = IsValidParameterType( dictionaryTypes[1], deserializerType );

				if( validKeyType && validValueType ) {
					return true;
				}
			}
			return false;
		}

		private static bool HasConstructorDeserializer( Type type, Type deserializerType ) {
			ConstructorInfo deserializer = type.GetConstructor(
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
						binder: null,
						types: new[] { deserializerType },
						modifiers: null
					);
			return deserializer != null;
		}

		private static bool IsDeserializable( Type type, Type deserializerType ) {
			return deserializerType.IsAssignableFrom( type );
		}
	}
}
