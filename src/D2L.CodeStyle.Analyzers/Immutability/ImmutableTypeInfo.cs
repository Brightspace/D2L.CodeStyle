using System;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// A representation of the immutability of some type.
	/// </summary>
	internal readonly struct ImmutableTypeInfo : IEquatable<ImmutableTypeInfo> {
		public ImmutableTypeInfo(
			ImmutableTypeKind kind,
			ITypeSymbol type
		) {
			Kind = kind;
			Type = type;
		}

		public ImmutableTypeKind Kind { get; }

		public ITypeSymbol Type { get; }

		public bool Equals( ImmutableTypeInfo other ) => this.Equals( other );

		public override bool Equals( object obj )
			=> obj is ImmutableTypeInfo other
				&& other.Kind == Kind
				&& other.Type == Type;

		public override int GetHashCode() {
			int hash = 17;
			hash = (hash * 31) + Kind.GetHashCode();
			hash = (hash * 31) + Type.GetHashCode();
			return hash;
		}
	}
}
