using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analysis {

	public sealed class MutabilityInspector {

		/// <summary>
		/// A list of known non-valuetype immutable types
		/// </summary>
		private static readonly ImmutableHashSet<string> KnownImmutableTypes = new HashSet<string> {
			nameof(String)
		}.ToImmutableHashSet();

		/// <summary>
		/// Determine if a given type is mutable.
		/// </summary>
		/// <param name="type">The type to determine mutability for.</param>
		/// <returns>Whether the type is mutable.</returns>
		public bool IsTypeMutable(
			ITypeSymbol type
		) {
			if( type.IsValueType ) {
				return false;
			}

			if( type.TypeKind == TypeKind.Array ) {
				return true;
			}

			if( KnownImmutableTypes.Contains( type.Name ) ) {
				return false;
			}

			foreach( var member in type.GetMembers() ) {
				if( member is IPropertySymbol && IsPropertyMutable( member as IPropertySymbol ) ) {
					return true;
				}
				if( member is IFieldSymbol && IsFieldMutable( member as IFieldSymbol ) ) {
					return true;
				}
				// skip methods, events, ctors, etc.
			}

			return false;
		}

		/// <summary>
		/// Determine if a property is mutable.
		/// This does not check if the type of the property is also mutable; use <see cref="IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="prop">The property to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsPropertyMutable( IPropertySymbol prop ) {
			if( prop.IsReadOnly ) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if a field is mutable.
		/// This does not check if the type of the field is also mutable; use <see cref="IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="field">The field to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsFieldMutable( IFieldSymbol field ) {
			if( field.IsReadOnly ) {
				return false;
			}
			return true;
		}

	}
}
