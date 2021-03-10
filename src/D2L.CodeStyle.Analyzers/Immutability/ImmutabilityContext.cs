﻿using System;
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
	internal sealed partial class ImmutabilityContext {
		private readonly ImmutableDictionary<INamedTypeSymbol, ImmutableTypeInfo> m_extraImmutableTypes;
		private readonly ImmutableHashSet<ITypeParameterSymbol> m_conditionalTypeParameters;

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

		private ImmutabilityContext(
			ImmutableDictionary<INamedTypeSymbol, ImmutableTypeInfo> extraImmutableTypes,
			ImmutableHashSet<ITypeParameterSymbol> conditionalTypeParamemters
		) {
			m_extraImmutableTypes = extraImmutableTypes;
			m_conditionalTypeParameters = conditionalTypeParamemters;
		}

		/// <summary>
		/// This function is intended to provide a new immutability context that considers the conditional type parameters
		/// of the ConditionallyImmutable type being checked as immutable. It is important this is only used in those cases,
		/// and not for instance while validating type arguments to an [Immutable] type parameter.
		/// </summary>
		/// <param name="type">The ConditionallyImmutable type definition being checked</param>
		/// <returns></returns>
		public ImmutabilityContext WithConditionalTypeParametersAsImmutable(
			INamedTypeSymbol type
		) {
			if( !Attributes.Objects.ConditionallyImmutable.IsDefined( type ) ) {
				throw new InvalidOperationException( $"{nameof( WithConditionalTypeParametersAsImmutable )} should only be called on ConditionallyImmutable types" );
			}

			var conditionalTypeParameters = type.TypeParameters.Where( p => Attributes.Objects.OnlyIf.IsDefined( p ) );

			return new ImmutabilityContext(
				extraImmutableTypes: m_extraImmutableTypes,
				conditionalTypeParamemters: m_conditionalTypeParameters.Union( conditionalTypeParameters )
			);
		}

		/// <summary>
		/// Determines if a type is known to be immutable.
		/// </summary>
		/// <param name="query">The type to check (and what kind of check to do.)</param>
		/// <param name="diag">If this method returns false, an explaination for why its not known to be immutable.</param>
		/// <returns>Is the type immutable?</returns>
		public bool IsImmutable(
			ImmutabilityQuery query,
			Func<Location> getLocation,
			out Diagnostic diagnostic
		) {
			if ( query.Kind == ImmutableTypeKind.None ) {
				throw new ArgumentException(
					"ImmutabilityKind.None is not a valid question to ask this function",
					nameof( query.Kind )
				);
			}

			diagnostic = null;

			// Things like int are totally OK
			if ( m_totallyImmutableSpecialTypes.Contains( query.Type.SpecialType ) ) {
				return true;
			}

			// "new object()" are always immutable (and that's the only
			// constructor for System.Object) but in general things of type
			// System.Object (any reference type) may be mutable.
			//
			// This is hard-coded (rather than in m_extraImmutableTypes) to
			// avoid the ITypeSymbol lookup.
			if ( query.Kind == ImmutableTypeKind.Instance && query.Type.SpecialType == SpecialType.System_Object ) {
				return true;
			}

			if( query.Type is INamedTypeSymbol namedType ) {
				ImmutableTypeInfo info = GetImmutableTypeInfo( namedType );
				if( info.Kind.HasFlag( query.Kind ) ) {
					return info.IsImmutableDefinition(
						context: this,
						definition: namedType,
						getLocation: getLocation,
						out diagnostic
					);
				}
			}

			switch( query.Type.TypeKind ) {
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
						getLocation(),
						( query.Type as IArrayTypeSymbol ).ElementType.Name
					);

					return false;

				case TypeKind.Delegate:
					diagnostic = Diagnostic.Create(
						Diagnostics.DelegateTypesPossiblyMutable,
						getLocation()
					);

					return false;

				case TypeKind.Dynamic:
					diagnostic = Diagnostic.Create(
						Diagnostics.DynamicObjectsAreMutable,
						getLocation()
					);

					return false;

				case TypeKind.TypeParameter:
					if( GetImmutabilityFromAttributes( query.Type ).HasFlag( ImmutableTypeKind.Total ) ) {
						return true;
					}

					if( query.Type is ITypeParameterSymbol tp && m_conditionalTypeParameters.Contains( tp ) ) {
						return true;
					}

					diagnostic = Diagnostic.Create(
						Diagnostics.TypeParameterIsNotKnownToBeImmutable,
						getLocation(),
						query.Type.ToDisplayString()
					);

					return false;

				case TypeKind.Class:
					diagnostic = Diagnostic.Create(
						Diagnostics.NonImmutableTypeHeldByImmutable,
						getLocation(),
						query.Type.TypeKind.ToString().ToLower(),
						query.Type.ToDisplayString(),
						query.Kind == ImmutableTypeKind.Instance && !query.Type.IsSealed ? " (or [ImmutableBaseClass])" : ""
					);

					return false;

				case TypeKind.Interface:
				case TypeKind.Struct:
					diagnostic = Diagnostic.Create(
						Diagnostics.NonImmutableTypeHeldByImmutable,
						getLocation(),
						query.Type.TypeKind.ToString().ToLower(),
						query.Type.ToDisplayString(),
						""
					);

					return false;

				case TypeKind.Unknown:
				default:
					diagnostic = Diagnostic.Create(
						Diagnostics.UnexpectedTypeKind,
						location: getLocation(),
						query.Type.Kind
					);

					return false;
			}
		}

		public ImmutableTypeInfo GetImmutableTypeInfo(
			INamedTypeSymbol type
		) {
			// Check for [Immutable] etc.
			ImmutableTypeKind fromAttributes = GetImmutabilityFromAttributes( type );
			if( fromAttributes != ImmutableTypeKind.None ) {
				return ImmutableTypeInfo.Create(
					kind: fromAttributes,
					type: type
				);
			}

			if( type.IsTupleType ) {
				return ImmutableTypeInfo.CreateWithAllConditionalTypeParameters(
					kind: ImmutableTypeKind.Total,
					type: type.OriginalDefinition
				);
			}

			// Check if we were otherwise told that this type is immutable
			if( m_extraImmutableTypes.TryGetValue( type.OriginalDefinition, out ImmutableTypeInfo info ) ) {
				return info;
			}

			return ImmutableTypeInfo.Create(
				kind: ImmutableTypeKind.None,
				type: type
			);
		}

		private static ImmutableTypeKind GetImmutabilityFromAttributes(
			ITypeSymbol type
		) {
			if ( Attributes.Objects.Immutable.IsDefined( type ) ) {
				return ImmutableTypeKind.Total;
			}

			if( Attributes.Objects.ConditionallyImmutable.IsDefined( type ) ) {
				return ImmutableTypeKind.Total;
			}

			if ( Attributes.Objects.ImmutableBaseClass.IsDefined( type ) ) {
				return ImmutableTypeKind.Instance;
			}

			return ImmutableTypeKind.None;
		}
	}
}
