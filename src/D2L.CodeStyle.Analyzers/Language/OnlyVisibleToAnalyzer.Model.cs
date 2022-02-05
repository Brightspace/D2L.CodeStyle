using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Language {

	public sealed partial class OnlyVisibleToAnalyzer {

		internal readonly struct Model {

			private const string OnlyVisibleToAttributeMetadataName = "D2L.CodeStyle.Annotations.Contract.OnlyVisibleToAttribute";

			private readonly Compilation m_compilation;
			private readonly INamedTypeSymbol m_onlyVisibleToAttribute;

			private readonly ConcurrentDictionary<ISymbol, ImmutableHashSet<INamedTypeSymbol>?> m_visibilityCache =
				new ConcurrentDictionary<ISymbol, ImmutableHashSet<INamedTypeSymbol>?>( SymbolEqualityComparer.Default );

			private Model(
					Compilation compilation,
					INamedTypeSymbol onlyVisibleToAttribute
				) {

				m_compilation = compilation;
				m_onlyVisibleToAttribute = onlyVisibleToAttribute;
			}

			public static Model? TryCreate( Compilation compilation ) {

				INamedTypeSymbol? onlyVisibleToAttribute = compilation.GetTypeByMetadataName( OnlyVisibleToAttributeMetadataName );
				if( onlyVisibleToAttribute == null ) {
					return null;
				}

				return new Model( compilation, onlyVisibleToAttribute );
			}

			public bool IsVisibleTo( INamedTypeSymbol caller, ISymbol member ) {

				ImmutableHashSet<INamedTypeSymbol>? restrictions = m_visibilityCache
					.GetOrAdd( member, GetVisibilityRestrictions );
	
				if( restrictions == null ) {
					return true;
				}

				return restrictions.Contains( caller );
			}

			private ImmutableHashSet<INamedTypeSymbol>? GetVisibilityRestrictions( ISymbol member ) {

				ImmutableArray<AttributeData> attributes = member.GetAttributes();

				int attributeCount = attributes.Length;
				if( attributeCount == 0 ) {
					return null;
				}

				ImmutableHashSet<INamedTypeSymbol>.Builder? restrictions = null;

				for( int i = 0; i < attributeCount; i++ ) {
					AttributeData attribute = attributes[ i ];

					if( SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, m_onlyVisibleToAttribute ) ) {

						if( restrictions == null ) {
							restrictions = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( SymbolEqualityComparer.Default );
						}

						INamedTypeSymbol? caller = TryGetCallerRestriction( attribute );
						if( caller != null ) {
							restrictions.Add( caller );
						}
					}
				}

				// null implies that we never saw any [OnlyVisibleTo] attributes
				if( restrictions == null ) {
					return null;
				}

				// could be empty in the case where we saw an [OnlyVisibleTo] attribute but the type is not in compilation
				return restrictions.ToImmutable();
			}

			private INamedTypeSymbol? TryGetCallerRestriction( AttributeData attribute ) {

				if( attribute.ConstructorArguments.Length != 1 ) {
					return null;
				}

				TypedConstant metadataNameArgument = attribute.ConstructorArguments[ 0 ];
				if( metadataNameArgument.Value is not string metadataName ) {
					return null;
				}

				return m_compilation.GetTypeByMetadataName( metadataName );
			}
		}
	}
}
