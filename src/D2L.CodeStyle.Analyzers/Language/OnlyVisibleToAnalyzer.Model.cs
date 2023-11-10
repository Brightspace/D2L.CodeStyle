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
				INamedTypeSymbol? onlyVisibleToTypeAttribute = compilation
					.GetTypeByMetadataName( OnlyVisibleToTypeAttributeMetadataName );

				if( onlyVisibleToTypeAttribute == null ) {
					return null;
				}

				return new Model( compilation, onlyVisibleToTypeAttribute );
			}

			public bool IsVisibleTo( INamedTypeSymbol caller, ISymbol symbol ) {

				ImmutableHashSet<INamedTypeSymbol>? restrictions = m_visibilityCache
					.GetOrAdd( symbol, GetSymbolVisibilityRestrictions );

				if( restrictions == null ) {
					return true;
				}

				if( restrictions.Contains( caller ) ) {
					return true;
				}

				if( SymbolEqualityComparer.Default.Equals( caller, symbol.ContainingType ) ) {
					return true;
				}

				return false;
			}

			private ImmutableHashSet<INamedTypeSymbol>? GetSymbolVisibilityRestrictions( ISymbol symbol ) {

				ImmutableHashSet<INamedTypeSymbol>.Builder? restrictions = null;

				// Local method to be able to add directly to the builder
				void AddTypeRestrictions(
					INamedTypeSymbol attributeSymbol,
					Compilation compilation,
					ISymbol symbol,
					bool excludeUninherited
				) {
					// Get all attributes on the specified symbol
					ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
					if( attributes.IsEmpty ) {
						return;
					}

					foreach( AttributeData attribute in attributes ) {
						// Ignore any attributes that are not the relevant attribute
						if( !SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, attributeSymbol ) ) {
							continue;
						}

						restrictions ??= ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(
							SymbolEqualityComparer.Default
						);

						// Retrieve the referenced type and inheritance from the attribute
						AttributeInfo? attributeInfo = GetAttributeInfo( attribute, compilation );

						// Ignore any attributes which could not be retrieved
						if( attributeInfo == null ) {
							continue;
						}

						// Conditionally ignore any attributes which are not inherited
						// (if `Foo : IBar`, this prevents references of `Foo` from
						// throwing an error if `IBar` has restrictions that are not inherited)
						if( excludeUninherited && !attributeInfo.Value.IsInherited ) {
							continue;
						}

						restrictions.Add( attributeInfo.Value.Symbol );
					}
				}

				// Get any restrictions that are explicitly on the symbol
				AddTypeRestrictions(
					attributeSymbol: m_onlyVisibleToTypeAttribute,
					compilation: m_compilation,
					symbol: symbol,
					excludeUninherited: false
				);

				// Only named types have bases, so if the symbol is
				// a named type then we need to look at its bases
				if( symbol is INamedTypeSymbol namedTypeSymbol ) {

					// Check visibility against all base classes
					INamedTypeSymbol? baseType = namedTypeSymbol.BaseType;
					while( baseType is not null ) {
						AddTypeRestrictions(
							attributeSymbol: m_onlyVisibleToTypeAttribute,
							compilation: m_compilation,
							symbol: baseType,
							excludeUninherited: true
						);
						baseType = baseType.BaseType;
					}

					// Check visibility against all implemented interfaces
					foreach( INamedTypeSymbol interfaceSymbol in namedTypeSymbol.AllInterfaces ) {
						AddTypeRestrictions(
							attributeSymbol: m_onlyVisibleToTypeAttribute,
							compilation: m_compilation,
							symbol: interfaceSymbol,
							excludeUninherited: true
						);
					}
				}

				// Null implies that we never saw any [OnlyVisibleToType] attributes
				if( restrictions == null ) {
					return null;
				}

				// Could be empty in the case where we saw an [OnlyVisibleTo] attribute
				// but none of the indicated types are in this compilation
				return restrictions.ToImmutable();
			}

			private static AttributeInfo? GetAttributeInfo(
				AttributeData attribute,
				Compilation compilation
			) {
				ImmutableArray<TypedConstant> attributeArgs = attribute.ConstructorArguments;
				return attributeArgs.Length switch {
					2 => GetFullyTypedAttributeInfo( attributeArgs ),
					3 => GetQualifiedAttributeInfo( attributeArgs, compilation ),
					_ => null,
				};
			}

			private static AttributeInfo? GetFullyTypedAttributeInfo(
				ImmutableArray<TypedConstant> arguments
			) {
				TypedConstant typeArgument = arguments[0];
				if( typeArgument.Value is not INamedTypeSymbol type ) {
					return null;
				}

				TypedConstant inheritedArgument = arguments[1];
				if( inheritedArgument.Value is not bool inherited ) {
					return null;
				}

				if( type.IsUnboundGenericType ) {
					return new AttributeInfo {
						Symbol = type.OriginalDefinition,
						IsInherited = inherited
					};
				}

				return new AttributeInfo {
					Symbol = type,
					IsInherited = inherited
				};
			}

			private static AttributeInfo? GetQualifiedAttributeInfo(
				ImmutableArray<TypedConstant> arguments,
				Compilation compilation
			) {
				TypedConstant metadataNameArgument = arguments[0];
				if( metadataNameArgument.Value is not string metadataName ) {
					return null;
				}

				TypedConstant assemblyNameArgument = arguments[1];
				if( assemblyNameArgument.Value is not string assemblyName ) {
					return null;
				}

				INamedTypeSymbol? type = compilation.GetTypeByMetadataName( metadataName );
				if( type == null ) {
					return null;
				}

				TypedConstant inheritedArgument = arguments[2];
				if( inheritedArgument.Value is not bool inherited ) {
					return null;
				}

				if( !type.ContainingAssembly.Name.Equals( assemblyName, StringComparison.Ordinal ) ) {
					return null;
				}

				return new AttributeInfo {
					Symbol = type,
					IsInherited = inherited
				};
			}

			private readonly struct AttributeInfo {
				public INamedTypeSymbol Symbol { get; init; }
				public bool IsInherited { get; init; }
			}
		}
	}
}
