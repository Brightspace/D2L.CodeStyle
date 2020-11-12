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
		// type to be immutable
		private readonly ImmutableArray<bool> m_immutableTypeParameters;

		private ImmutableTypeInfo(
			ImmutableTypeKind kind,
			INamedTypeSymbol type,
			ImmutableArray<bool> immutableTypeParameters
		) {
			Kind = kind;
			Type = type;
			m_immutableTypeParameters = immutableTypeParameters;
		}

		public ImmutableTypeKind Kind { get; }

		public INamedTypeSymbol Type { get; }

		public bool IsImmutableDefinition(
			ImmutabilityContext context,
			INamedTypeSymbol definition,
			Location location,
			out Diagnostic diagnostic
		) {
			if( !Type.Equals( definition ) && !Type.Equals( definition?.OriginalDefinition ) ) {
				throw new InvalidOperationException( $"{ nameof( IsImmutableDefinition ) } should only be called with an equivalent type definition" );
			}

			var argRelevance = definition
				.TypeArguments
				.Zip( m_immutableTypeParameters, ( a, relevant ) => (a, relevant) );
			foreach( (ITypeSymbol argument, bool isRelevant) in argRelevance ) {
				if( !isRelevant ) {
					continue;
				}

				if( !context.IsImmutable( argument, ImmutableTypeKind.Total, location, out diagnostic ) ) {
					return false;
				}
			}

			diagnostic = null;
			return true;
		}

		public static ImmutableTypeInfo Create(
			ImmutableTypeKind kind,
			INamedTypeSymbol type
		) {
			ImmutableArray<bool> immutableTypeParameters = type
				.TypeParameters
				.Select( p => Attributes.Objects.Immutable.IsDefined( p ) )
				.ToImmutableArray();

			return new ImmutableTypeInfo(
				kind: kind,
				type: type,
				immutableTypeParameters: immutableTypeParameters
			);
		}

		public static ImmutableTypeInfo CreateWithAllImmutableTypeParameters(
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
				immutableTypeParameters: immutableTypeParameters
			);
		}
	}
}
