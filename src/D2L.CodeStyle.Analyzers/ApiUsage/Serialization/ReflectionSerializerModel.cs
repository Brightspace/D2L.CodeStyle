using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	internal sealed class ReflectionSerializerModel {

		private readonly INamedTypeSymbol m_ignoreAttributeType;
		private readonly INamedTypeSymbol m_reflectionSerializerAttributeType;

		public ReflectionSerializerModel(
				INamedTypeSymbol ignoreAttributeType,
				INamedTypeSymbol reflectionSerializerAttributeType
			) {

			m_ignoreAttributeType = ignoreAttributeType;
			m_reflectionSerializerAttributeType = reflectionSerializerAttributeType;
		}

		public ImmutableArray<IMethodSymbol> GetOrderedPublicInstanceConstructors( INamedTypeSymbol type ) {

			var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();

			foreach( IMethodSymbol constructor in type.InstanceConstructors ) {

				if( constructor.DeclaredAccessibility == Accessibility.Public ) {

					if( constructor.Parameters.IsEmpty ) {

						// empty constructor takes priority over all others
						builder.Insert( 0, constructor );

					} else {
						builder.Add( constructor );
					}
				}
			}

			return builder.ToImmutable();
		}

		public ImmutableHashSet<string> GetPublicReadablePropertyNames( INamedTypeSymbol type ) {

			var propertyNames = ImmutableHashSet.CreateBuilder( StringComparer.OrdinalIgnoreCase );

			INamedTypeSymbol? currentType = type;
			do {
				ImmutableArray<ISymbol> members = currentType.GetMembers();

				IEnumerable<IPropertySymbol> properties = members
					.OfType<IPropertySymbol>()
					.Where( IsPublicReadableProperty );

				foreach( IPropertySymbol property in properties ) {
					propertyNames.Add( property.Name );
				}

				currentType = currentType.BaseType;

			} while( currentType != null );

			return propertyNames.ToImmutable();
		}

		private bool IsPublicReadableProperty( IPropertySymbol property ) {

			if( property.DeclaredAccessibility != Accessibility.Public ) {
				return false;
			}

			if( property.IsStatic ) {
				return false;
			}

			if( property.IsIndexer ) {
				return false;
			}

			if( property.IsWriteOnly ) {
				return false;
			}

			if( HasIgnoreAttribute( property ) ) {
				return false;
			}

			IMethodSymbol? getMethod = property.GetMethod;
			if( getMethod == null ) {
				return false;
			}

			if( HasIgnoreAttribute( getMethod ) ) {
				return false;
			}

			return true;
		}

		private bool HasIgnoreAttribute( ISymbol symbol ) {
			return HasAttributeOfType( symbol, m_ignoreAttributeType, out _ );
		}

		public bool HasReflectionSerializerAttribute( INamedTypeSymbol symbol, [NotNullWhen( true )] out AttributeData? attribute ) {
			return HasAttributeOfType( symbol, m_reflectionSerializerAttributeType, out attribute );
		}

		private bool HasAttributeOfType(
				ISymbol symbol,
				INamedTypeSymbol type,
				[NotNullWhen( true )] out AttributeData? attributeData
			) {

			ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
			foreach( AttributeData attribute in attributes ) {

				if( SymbolEqualityComparer.Default.Equals(
						attribute.AttributeClass,
						type
					) ) {

					attributeData = attribute;
					return true;
				}
			}

			attributeData = null;
			return false;
		}
	}
}
