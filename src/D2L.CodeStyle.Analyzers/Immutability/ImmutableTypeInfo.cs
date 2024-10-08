#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// A representation of the immutability of some type.
	/// </summary>
	internal readonly struct ImmutableTypeInfo {

		// a mapping of which type parameters considered necessarily immutable for the
		// conditionally immutable type to be immutable
		private readonly ImmutableArray<bool> m_conditionalTypeParameters;

		private ImmutableTypeInfo(
			ImmutableTypeKind kind,
			INamedTypeSymbol type,
			ImmutableArray<bool> conditionalTypeParameters
		) {
			Kind = kind;
			Type = type;
			m_conditionalTypeParameters = conditionalTypeParameters;

			IsConditional = m_conditionalTypeParameters.Any( isConditional => isConditional );
		}

		public ImmutableTypeKind Kind { get; }

		public INamedTypeSymbol Type { get; }

		public bool IsConditional { get; }

		public IEnumerable<(ITypeParameterSymbol TypeParameter, int OriginalOrdinal)> GetConditionalTypeParameters() {
			if( !IsConditional ) {
				yield break;
			}

			for( int i = 0; i < Type.TypeParameters.Length; i++ ) {
				if( !m_conditionalTypeParameters[ i ] ) {
					continue;
				}

				yield return (Type.TypeParameters[ i ], i);
			}
		}

		public bool IsImmutableDefinition(
			ImmutabilityContext context,
			INamedTypeSymbol definition,
			Func<Location> getLocation,
			out Diagnostic diagnostic
		) {
			if( !Type.Equals( definition, SymbolEqualityComparer.Default )
				&& !Type.Equals( definition?.OriginalDefinition, SymbolEqualityComparer.Default )
			) {
				throw new InvalidOperationException( $"{ nameof( IsImmutableDefinition ) } should only be called with an equivalent type definition" );
			}

			var argRelevance = definition
				.TypeArguments
				.Zip( m_conditionalTypeParameters, ( a, relevant ) => (a, relevant) );
			foreach( (ITypeSymbol argument, bool isRelevant) in argRelevance ) {
				if( !isRelevant ) {
					continue;
				}

				if( !context.IsImmutable(
					new ImmutabilityQuery(
						ImmutableTypeKind.Total,
						argument
					),
					getLocation,
					out diagnostic
				) ) {
					return false;
				}
			}

			diagnostic = null;
			return true;
		}

		public static ImmutableTypeInfo Create(
			AnnotationsContext annotationsContext,
			ImmutableTypeKind kind,
			INamedTypeSymbol type
		) {
			ImmutableArray<bool> immutableTypeParameters = type
				.TypeParameters
				.Select( p => annotationsContext.Objects.OnlyIf.IsDefined( p ) )
				.ToImmutableArray();

			return new ImmutableTypeInfo(
				kind: kind,
				type: type,
				conditionalTypeParameters: immutableTypeParameters
			);
		}

		public static ImmutableTypeInfo CreateWithAllConditionalTypeParameters(
			ImmutableTypeKind kind,
			INamedTypeSymbol type
		) {
			ImmutableArray<bool> immutableTypeParameters = type
				.TypeParameters
				.Select( p => true )
				.ToImmutableArray();

			return new ImmutableTypeInfo(
				kind: kind,
				type: type,
				conditionalTypeParameters: immutableTypeParameters
			);
		}
	}
}
