#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	internal sealed class DangerousMembersModel {

		private readonly record struct AuditAttributePair(
				INamedTypeSymbol? Audited,
				INamedTypeSymbol? Unaudited
			);

		private const string DangerousMethodAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+AuditedAttribute";
		private const string DangerousMethodUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousMethodUsage+UnauditedAttribute";

		private const string DangerousPropertyAuditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+AuditedAttribute";
		private const string DangerousPropertyUnauditedAttributeFullName = "D2L.CodeStyle.Annotations.DangerousPropertyUsage+UnauditedAttribute";

		private readonly ImmutableHashSet<ISymbol> m_dangerousMembers;
		private readonly AuditAttributePair m_dangerousMethodAttributes;
		private readonly AuditAttributePair m_dangerousPropertyAttributes;

		public bool HasMethods { get; }
		public bool HasProperties { get; }

		private DangerousMembersModel(
				ImmutableHashSet<ISymbol> dangerousMembers,
				AuditAttributePair dangerousMethodAttributes,
				AuditAttributePair dangerousPropertyAttributes,
				bool hasMethods,
				bool hasProperties
			) {

			m_dangerousMembers = dangerousMembers;
			m_dangerousMethodAttributes = dangerousMethodAttributes;
			m_dangerousPropertyAttributes = dangerousPropertyAttributes;

			HasMethods = hasMethods;
			HasProperties = hasProperties;
		}

		public bool IsDangerousMethod( ISymbol containingSymbol, IMethodSymbol method ) {
			return IsDangerousMember( containingSymbol, method, m_dangerousMethodAttributes );
		}

		public bool IsDangerousProperty( ISymbol containingSymbol, IPropertySymbol property ) {
			return IsDangerousMember( containingSymbol, property, m_dangerousPropertyAttributes );
		}

		private bool IsDangerousMember(
				ISymbol containingSymbol,
				ISymbol member,
				AuditAttributePair auditAttributes
			) {

			if( !IsWellKnownDangerousMember( member ) ) {
				return false;
			}

			ImmutableArray<AttributeData> attributes = containingSymbol.GetAttributes();
			foreach( AttributeData attribute in attributes ) {

				if( IsAuditAttribute( member, attribute, auditAttributes ) ) {
					return false;
				}
			}

			return true;
		}

		private bool IsWellKnownDangerousMember( ISymbol memberSymbol ) {

			ISymbol originalDefinition = memberSymbol.OriginalDefinition;

			if( m_dangerousMembers.Contains( originalDefinition ) ) {
				return true;
			}

			if( !memberSymbol.Equals( originalDefinition, SymbolEqualityComparer.Default ) ) {

				if( m_dangerousMembers.Contains( memberSymbol ) ) {
					return true;
				}
			}

			return false;
		}

		private bool IsAuditAttribute(
				ISymbol member,
				AttributeData attribute,
				AuditAttributePair auditAttributes
			) {

			INamedTypeSymbol? attributeClass = attribute.AttributeClass;
			if( attributeClass == null ) {
				return false;
			}

			bool isAudited = (
					attributeClass.Equals( auditAttributes.Audited, SymbolEqualityComparer.Default )
					|| attributeClass.Equals( auditAttributes.Unaudited, SymbolEqualityComparer.Default )
				);
			if( !isAudited ) {
				return false;
			}

			if( attribute.ConstructorArguments.Length < 2 ) {
				return false;
			}

			TypedConstant typeArg = attribute.ConstructorArguments[ 0 ];
			if( typeArg.Value == null ) {
				return false;
			}
			if( !member.ContainingType.Equals( typeArg.Value ) ) {
				return false;
			}

			TypedConstant nameArg = attribute.ConstructorArguments[ 1 ];
			if( nameArg.Value == null ) {
				return false;
			}
			if( !member.Name.Equals( nameArg.Value ) ) {
				return false;
			}

			return true;
		}

		public static DangerousMembersModel? TryCreate( Compilation compilation ) {

			ImmutableHashSet<ISymbol> dangerousMembers = GetDangerousMembers(
					compilation,
					out bool hasMethods,
					out bool hasProperties
				);

			if( dangerousMembers.IsEmpty ) {
				return null;
			}

			INamedTypeSymbol? dangerousMethodAuditedAttribute = compilation
				.GetTypeByMetadataName( DangerousMethodAuditedAttributeFullName );

			INamedTypeSymbol? dangerousMethodUnauditedAttribute = compilation
				.GetTypeByMetadataName( DangerousMethodUnauditedAttributeFullName );

			INamedTypeSymbol? dangerousPropertyAuditedAttribute = compilation
				.GetTypeByMetadataName( DangerousPropertyAuditedAttributeFullName );

			INamedTypeSymbol? dangerousPropertyUnauditedAttribute = compilation
				.GetTypeByMetadataName( DangerousPropertyUnauditedAttributeFullName );

			return new DangerousMembersModel(
					dangerousMembers,
					dangerousMethodAttributes: new(
						Audited: dangerousMethodAuditedAttribute,
						Unaudited: dangerousMethodUnauditedAttribute
					),
					dangerousPropertyAttributes: new(
						Audited: dangerousPropertyAuditedAttribute,
						Unaudited: dangerousPropertyUnauditedAttribute
					),
					hasMethods: hasMethods,
					hasProperties: hasProperties
				);
		}

		private static ImmutableHashSet<ISymbol> GetDangerousMembers(
				Compilation compilation,
				out bool hasMethods,
				out bool hasProperties
			) {

			var builder = ImmutableHashSet.CreateBuilder<ISymbol>( SymbolEqualityComparer.Default );

			IEnumerable<KeyValuePair<string, ImmutableArray<string>>> definitions = Enumerable
				.Concat(
					DangerousMethods.Definitions,
					DangerousProperties.Definitions
				);

			hasMethods = false;
			hasProperties = false;

			foreach( KeyValuePair<string, ImmutableArray<string>> pairs in definitions ) {

				INamedTypeSymbol? type = compilation.GetTypeByMetadataName( pairs.Key );
				if( type == null ) {
					continue;
				}

				foreach( string name in pairs.Value ) {

					ImmutableArray<ISymbol> members = type.GetMembers( name );
					foreach( ISymbol member in members ) {

						switch( member.Kind ) {

							case SymbolKind.Method:
								builder.Add( member );
								hasMethods = true;
								break;

							case SymbolKind.Property:
								builder.Add( member );
								hasProperties = true;
								break;
						}
					}
				}
			}

			return builder.ToImmutable();
		}
	}
}
