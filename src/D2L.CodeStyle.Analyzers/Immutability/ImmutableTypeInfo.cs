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

		private readonly ImmutableArray<(bool RequiresImmutability, bool IsImmutableCondition)> m_typeParameterInfo;

		private ImmutableTypeInfo(
			ImmutableTypeKind kind,
			INamedTypeSymbol type,
			ImmutableArray<(bool RequiresImmutability, bool IsImmutableCondition)> typeParameterInfo
		) {
			Kind = kind;
			Type = type;
			m_typeParameterInfo = typeParameterInfo;
		}

		public ImmutableTypeKind Kind { get; }

		public INamedTypeSymbol Type { get; }

		public bool IsConditional => m_typeParameterInfo.Any( p => p.IsImmutableCondition );

		public bool IsImmutableDefinition(
			ImmutabilityContext context,
			INamedTypeSymbol definition,
			bool enforceImmutableTypeParams,
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
				.Zip( m_typeParameterInfo, ( a, info ) => (a, info) );
			foreach( (ITypeSymbol argument, (bool requiresImmutability, bool isImmutableCondition)) in argRelevance ) {
				bool relevant = isImmutableCondition || ( requiresImmutability && enforceImmutableTypeParams );
				if( !relevant ) {
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
			ImmutableArray<(bool RequiresImmutability, bool IsImmutableCondition)> typeParameterInfo = type
				.TypeParameters
				.Select( p => (
					annotationsContext.Objects.Immutable.IsDefined( p ),
					annotationsContext.Objects.OnlyIf.IsDefined( p )
				) )
				.ToImmutableArray();

			return new ImmutableTypeInfo(
				kind: kind,
				type: type,
				typeParameterInfo: typeParameterInfo
			);
		}

		public static ImmutableTypeInfo CreateWithAllConditionalTypeParameters(
			ImmutableTypeKind kind,
			INamedTypeSymbol type
		) {
			ImmutableArray<(bool RequiresImmutability, bool IsImmutableCondition)> typeParameterInfo = type
				.TypeParameters
				.Select( p => (
					false,
					true
				) )
				.ToImmutableArray();

			return new ImmutableTypeInfo(
				kind: kind,
				type: type,
				typeParameterInfo: typeParameterInfo
			);
		}
	}
}
