using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Language {

	public sealed partial class OnlyVisibleToAnalyzer {

		internal readonly struct Model {

			private const string OnlyVisibleToTypeAttributeMetadataName = "D2L.CodeStyle.Annotations.Contract.OnlyVisibleToTypeAttribute";

			private readonly Compilation m_compilation;
			private readonly INamedTypeSymbol m_onlyVisibleToTypeAttribute;

			private readonly ConcurrentDictionary<ISymbol, ImmutableHashSet<INamedTypeSymbol>?> m_visibilityCache =
				new ConcurrentDictionary<ISymbol, ImmutableHashSet<INamedTypeSymbol>?>( SymbolEqualityComparer.Default );

			private Model(
				Compilation compilation,
				INamedTypeSymbol onlyVisibleToTypeAttribute
			) {
				m_compilation = compilation;
				m_onlyVisibleToTypeAttribute = onlyVisibleToTypeAttribute;
			}

			public static Model? TryCreate( Compilation compilation ) {

				INamedTypeSymbol? onlyVisibleToTypeAttribute = compilation.GetTypeByMetadataName( OnlyVisibleToTypeAttributeMetadataName );
				if( onlyVisibleToTypeAttribute == null ) {
					return null;
				}

				return new Model( compilation, onlyVisibleToTypeAttribute );
			}

			public bool IsVisibleTo( INamedTypeSymbol caller, ISymbol member ) {

				ImmutableHashSet<INamedTypeSymbol>? restrictions = m_visibilityCache
					.GetOrAdd( member, GetTypeVisibilityRestrictions );

				if( restrictions == null ) {
					return true;
				}

				if( restrictions.Contains( caller ) ) {
					return true;
				}

				if( SymbolEqualityComparer.Default.Equals( caller, member.ContainingType ) ) {
					return true;
				}

				return false;
			}

			private ImmutableHashSet<INamedTypeSymbol>? GetTypeVisibilityRestrictions( ISymbol member ) {

				ImmutableArray<AttributeData> attributes = member.GetAttributes();
				if( attributes.IsEmpty ) {
					return null;
				}

				ImmutableHashSet<INamedTypeSymbol>.Builder? restrictions = null;

				foreach( AttributeData attribute in attributes ) {

					if( SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, m_onlyVisibleToTypeAttribute ) ) {

						restrictions ??= ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( SymbolEqualityComparer.Default );

						INamedTypeSymbol? visibleToType = TryGetOnlyVisibleToType( attribute );
						if( visibleToType != null ) {

							restrictions.Add( visibleToType );
						}
					}
				}

				// null implies that we never saw any [OnlyVisibleToType] attributes
				if( restrictions == null ) {
					return null;
				}

				// could be empty in the case where we saw an [OnlyVisibleTo] attribute but none of the indicated types are in this compilation
				return restrictions.ToImmutable();
			}

			private INamedTypeSymbol? TryGetOnlyVisibleToType( AttributeData attribute ) {

				ImmutableArray<TypedConstant> arguments = attribute.ConstructorArguments;

				if( arguments.Length == 2 ) {

					TypedConstant metadataNameArgument = arguments[ 0 ];
					if( metadataNameArgument.Value is not string metadataName ) {
						return null;
					}

					TypedConstant assemblyNameAttribute = arguments[ 1 ];
					if( assemblyNameAttribute.Value is not string assemblyName ) {
						return null;
					}

					INamedTypeSymbol? type = m_compilation.GetTypeByMetadataName( metadataName );
					if( type == null ) {
						return null;
					}

					if( !type.ContainingAssembly.Name.Equals( assemblyName, StringComparison.Ordinal ) ) {
						return null;
					}

					return type;
				}

				if( arguments.Length == 1 ) {

					TypedConstant typeArgument = arguments[ 0 ];
					if( typeArgument.Value is not INamedTypeSymbol type ) {
						return null;
					}

					if( type.IsUnboundGenericType ) {
						return type.OriginalDefinition;
					}

					return type;
				}

				return null;
			}
		}
	}
}
