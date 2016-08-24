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
		/// Determine if a given type is immutable.
		/// </summary>
		/// <param name="type">The type to determine immutability for.</param>
		/// <returns>Whether the type is immutable.</returns>
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
		/// Determine if a property is externally immutable (private properties are considered immutable).
		/// This does not check if the type of the property is also immutable; use <see cref="MutabilityInspector.IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="prop">The property to check for mutability.</param>
		/// <returns>Determines whether the property is immutable.</returns>
		public bool IsPropertyMutable( IPropertySymbol prop ) {
			if( prop.DeclaredAccessibility == Accessibility.Private ) {
				return false;
			}
			if( prop.IsReadOnly ) {
				return false;
			}
			if( prop.SetMethod.DeclaredAccessibility == Accessibility.Private ) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if a field is externally immutable (private fields are considered immutable).
		/// This does not check if the type of the field is also immutable; use <see cref="MutabilityInspector.IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="field">The field to check for mutability.</param>
		/// <returns>Determines whether the property is immutable.</returns>
		public bool IsFieldMutable( IFieldSymbol field ) {
			if( field.DeclaredAccessibility == Accessibility.Private ) {
				return false;
			}
			if( field.IsReadOnly ) {
				return false;
			}
			return true;
		}

	}
}
