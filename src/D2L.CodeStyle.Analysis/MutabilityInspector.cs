using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Linq;
using D2L.CodeStyle.Annotations;

namespace D2L.CodeStyle.Analysis {

	public sealed class MutabilityInspector {

		/// <summary>
		/// A list of known non-valuetype immutable types
		/// </summary>
		private static readonly ImmutableHashSet<string> KnownImmutableTypes = new HashSet<string> {
			"System.String",
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

			if( KnownImmutableTypes.Contains( type.GetFullTypeName() ) ) {
				return false;
			}

			foreach( var member in type.GetMembers() ) {
				if( member is IPropertySymbol ) {
					var prop = member as IPropertySymbol;
					if( IsPropertyMutable( prop ) || IsTypeMutable( prop.Type ) ) {
						return true;
					}
					continue;
				}
				if( member is IFieldSymbol ) {
					var field = member as IFieldSymbol;
					if( IsFieldMutable( field ) || IsTypeMutable( field.Type ) ) {
						return true;
					}
					continue;
				}
				if( member is IMethodSymbol ) {
					var method = member as IMethodSymbol;
					if( method.MethodKind == MethodKind.Constructor ) {
						// constructors are mutating by definition
						continue;
					}

					// we can't yet be smarter by methods being "pure"
					return true;
				}

				// we've got a member (event, etc.) that we can't currently be smart about, so fail
				return true;
			}

			return false;
		}

		public bool IsTypeMarkedImmutable( ITypeSymbol symbol ) {
			if( symbol.GetAttributes().Any( a => a.AttributeClass.Name == nameof( Objects.Immutable ) ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedImmutable ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedImmutable( symbol.BaseType ) ) {
				return true;
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
