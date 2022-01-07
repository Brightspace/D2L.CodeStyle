using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	internal sealed class ReflectionSerializerModel {

		private readonly SemanticModel m_model;
		private readonly INamedTypeSymbol m_reflectionSerializerAttributeType;
		private readonly INamedTypeSymbol m_ignoreAttributeType;

		public ReflectionSerializerModel(
				SemanticModel model,
				INamedTypeSymbol reflectionSerializerAttributeType,
				INamedTypeSymbol ignoreAttributeType
			) {

			m_model = model;
			m_reflectionSerializerAttributeType = reflectionSerializerAttributeType;
			m_ignoreAttributeType = ignoreAttributeType;
		}

		public bool IsReflectionSerializerAttribute( AttributeSyntax attribute ) {
			return m_model.IsAttributeOfType( attribute, m_reflectionSerializerAttributeType );
		}

		public ImmutableHashSet<string> GetPublicReadablePropertyNames( INamedTypeSymbol type ) {

			var propertyNames = ImmutableHashSet.CreateBuilder( StringComparer.OrdinalIgnoreCase );

			INamedTypeSymbol currentType = type;
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

			if( HasIgnoreAttribute( property.GetAttributes() ) ) {
				return false;
			}

			IMethodSymbol getMethod = property.GetMethod;
			if( getMethod == null ) {
				return false;
			}

			if( HasIgnoreAttribute( getMethod.GetAttributes() ) ) {
				return false;
			}

			return true;
		}

		private bool HasIgnoreAttribute( ImmutableArray<AttributeData> attributes ) {

			foreach( AttributeData attribute in attributes ) {

				if( SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, m_ignoreAttributeType ) ) {
					return true;
				}
			}

			return false;
		}
	}
}
