using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// A representation of the immutability of some type.
	/// </summary>
	internal readonly struct ImmutableTypeInfo : IEquatable<ImmutableTypeInfo> {

		private ImmutableTypeInfo(
			ImmutableTypeKind kind,
			INamedTypeSymbol type,
			ImmutableArray<bool> immutableTypeParameters
		) {
			Kind = kind;
			Type = type;
			ImmutableTypeParameters = immutableTypeParameters;
		}

		public ImmutableTypeKind Kind { get; }

		public INamedTypeSymbol Type { get; }

		public ImmutableArray<bool> ImmutableTypeParameters { get; }

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


		public bool Equals( ImmutableTypeInfo other ) => this.Equals( other );

		public override bool Equals( object obj )
			=> obj is ImmutableTypeInfo other
				&& other.Kind == Kind
				&& other.Type == Type
				&& other.ImmutableTypeParameters.Equals( ImmutableTypeParameters );

		public override int GetHashCode() {
			int hash = 17;
			hash = (hash * 31) + Kind.GetHashCode();
			hash = (hash * 31) + Type.GetHashCode();
			hash = (hash * 31) + ImmutableTypeParameters.GetHashCode();
			return hash;
		}
	}
}
