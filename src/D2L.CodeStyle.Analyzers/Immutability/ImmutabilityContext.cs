using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// Used to answer "do we know if this type immutable?" (in whichever sense).
	/// Generally this means checking for [Immutable] etc., but there are also
	/// some pre-approved types (like int) and other logic that this class
	/// organizes.
	///
	/// This doesn't do anything particularly smart: e.g. for a user defined
	/// class it is mostly just looking for [Immutable]. If this judges a type
	/// as not known to be immutable that doesn't mean it is mutable.
	/// </summary>
	internal sealed class ImmutabilityContext {
		private readonly ImmutableDictionary<INamedTypeSymbol, ImmutableTypeInfo> m_extraImmutableTypes = null;

		// Hard code this to avoid looking up the ITypeSymbol to include it in m_extraImmutableTypes
		private static readonly ImmutableHashSet<SpecialType> m_totallyImmutableSpecialTypes = ImmutableHashSet.Create(
			SpecialType.System_Enum,
			SpecialType.System_Boolean,
			SpecialType.System_Char,
			SpecialType.System_SByte,
			SpecialType.System_Byte,
			SpecialType.System_Int16,
			SpecialType.System_UInt16,
			SpecialType.System_Int32,
			SpecialType.System_UInt32,
			SpecialType.System_Int64,
			SpecialType.System_UInt64,
			SpecialType.System_Decimal,
			SpecialType.System_Single,
			SpecialType.System_Double,
			SpecialType.System_String,
			SpecialType.System_IntPtr,
			SpecialType.System_UIntPtr,
			SpecialType.System_ValueType
		);

		public ImmutabilityContext( IEnumerable<ImmutableTypeInfo> extraImmutableTypes ) {
			m_extraImmutableTypes = extraImmutableTypes
				.ToImmutableDictionary(
					info => info.Type,
					info => info
				);
		}

		/// <summary>
		/// Determines if a type is known to be immutable.
		/// </summary>
		/// <param name="type">The type to check</param>
		/// <param name="kind">The degree of immutability</param>
		/// <param name="diag">If this method returns false, an explaination for why its not known to be immutable.</param>
		/// <returns>Is the type immutable?</returns>
		public bool IsImmutable(
			ITypeSymbol type,
			ImmutableTypeKind kind,
			Location location,
			out Diagnostic diagnostic
		) {
			if ( kind == ImmutableTypeKind.None ) {
				throw new ArgumentException(
					"ImmutabilityKind.None is not a valid question to ask this function",
					nameof( kind )
				);
			}

			diagnostic = null;

			// Things like int are totally OK
			if ( m_totallyImmutableSpecialTypes.Contains( type.SpecialType ) ) {
				return true;
			}

			// "new object()" are always immutable (and that's the only
			// constructor for System.Object) but in general things of type
			// System.Object (any reference type) may be mutable.
			//
			// This is hard-coded (rather than in m_extraImmutableTypes) to
			// avoid the ITypeSymbol lookup.
			if ( kind == ImmutableTypeKind.Instance && type.SpecialType == SpecialType.System_Object ) {
				return true;
			}

			if( type is INamedTypeSymbol namedType ) {
				if( TryGetImmutableTypeInfo( namedType, out ImmutableTypeInfo info ) ) {
					if( info.Kind.HasFlag( kind ) ) {
						return info.IsImmutableDefinition(
							context: this,
							definition: namedType,
							location: location,
							out diagnostic
						);
					}
				}
			}

			switch( type.TypeKind ) {
				case TypeKind.Error:
					// Just say this is fine -- there is some other compiler
					// error in this case and we don't need to pile on.
					return true;

				case TypeKind.Enum:
					// Enums are like ints -- always immutable
					return true;

				case TypeKind.Array:
					diagnostic = Diagnostic.Create(
						Diagnostics.ArraysAreMutable,
						location,
						type.Name
					);

					return false;

				case TypeKind.Delegate:
					diagnostic = Diagnostic.Create(
						Diagnostics.DelegateTypesPossiblyMutable,
						location
					);

					return false;

				case TypeKind.Dynamic:
					diagnostic = Diagnostic.Create(
						Diagnostics.DynamicObjectsAreMutable,
						location
					);

					return false;

				case TypeKind.TypeParameter:
					// We already checked m_extraImmutableTypes above.
					diagnostic = Diagnostic.Create(
						Diagnostics.TypeParameterIsNotKnownToBeImmutable,
						location,
						type.Name
					);

					return false;

				case TypeKind.Interface:
				case TypeKind.Class:
				case TypeKind.Struct:
					diagnostic = Diagnostic.Create(
						Diagnostics.NonImmutableTypeHeldByImmutable,
						location,
						type.TypeKind,
						type.Name,
						kind == ImmutableTypeKind.Instance ? " (or [ImmutableBaseClass])" : ""
					);

					return false;

				case TypeKind.Unknown:
				default:
					diagnostic = Diagnostic.Create(
						Diagnostics.UnexpectedTypeKind,
						location: location,
						type.Kind
					);

					return false;
			}
		}

		private bool TryGetImmutableTypeInfo(
			INamedTypeSymbol type,
			out ImmutableTypeInfo info
		) {
			// Check for [Immutable] etc.
			ImmutableTypeKind fromAttributes = GetImmutabilityFromAttributes( type );
			if( fromAttributes != ImmutableTypeKind.None ) {
				info = ImmutableTypeInfo.Create(
					kind: fromAttributes,
					type: type
				);
				return true;
			}

			// Check if we were otherwise told that this type is immutable
			return m_extraImmutableTypes.TryGetValue( type, out info );
		}

		private static ImmutableTypeKind GetImmutabilityFromAttributes(
			ITypeSymbol type
		) {
			if ( Attributes.Objects.Immutable.IsDefined( type ) ) {
				return ImmutableTypeKind.Total;
			}

			if ( Attributes.Objects.ImmutableBaseClass.IsDefined( type ) ) {
				return ImmutableTypeKind.Instance;
			}

			return ImmutableTypeKind.None;
		}
	}
}
