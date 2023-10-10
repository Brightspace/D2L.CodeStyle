#nullable disable

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
	internal sealed partial class ImmutabilityContext {

		private readonly AnnotationsContext m_annotationsContext;
		private readonly ImmutableDictionary<INamedTypeSymbol, ImmutableTypeInfo> m_extraImmutableTypes;
		private readonly ImmutableHashSet<IMethodSymbol> m_knownImmutableReturns;
		private readonly ImmutableHashSet<ITypeParameterSymbol> m_conditionalTypeParameters;
		private readonly (INamedTypeSymbol RegexType, INamedTypeSymbol GeneratedCodeAttributeType) m_regexInfo;

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
			AnnotationsContext annotationsContext,
			ImmutableDictionary<INamedTypeSymbol, ImmutableTypeInfo> extraImmutableTypes,
			ImmutableHashSet<IMethodSymbol> knownImmutableReturns,
			ImmutableHashSet<ITypeParameterSymbol> conditionalTypeParamemters,
			(INamedTypeSymbol RegexType, INamedTypeSymbol GeneratedCodeAttributeType) regexInfo
		) {
			m_annotationsContext = annotationsContext;
			m_extraImmutableTypes = extraImmutableTypes;
			m_knownImmutableReturns = knownImmutableReturns;
			m_conditionalTypeParameters = conditionalTypeParamemters;
			m_regexInfo = regexInfo;
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
			if( !m_annotationsContext.Objects.ConditionallyImmutable.IsDefined( type ) ) {
				throw new InvalidOperationException( $"{nameof( WithConditionalTypeParametersAsImmutable )} should only be called on ConditionallyImmutable types" );
			}

			var conditionalTypeParameters = type.TypeParameters.Where( p => m_annotationsContext.Objects.OnlyIf.IsDefined( p ) );

			return new ImmutabilityContext(
				annotationsContext: m_annotationsContext,
				extraImmutableTypes: m_extraImmutableTypes,
				knownImmutableReturns: m_knownImmutableReturns,
				conditionalTypeParamemters: m_conditionalTypeParameters.Union( conditionalTypeParameters ),
				regexInfo: m_regexInfo
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
					nameof( query )
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
					string typeKindName = query.Type.TypeKind == TypeKind.Struct ? "structure" : query.Type.TypeKind.ToString().ToLower();
					diagnostic = Diagnostic.Create(
						Diagnostics.NonImmutableTypeHeldByImmutable,
						getLocation(),
						typeKindName,
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

		/// <summary>
		/// Determines if the return value of a method is known to be immutable.
		/// </summary>
		/// <param name="methodSymbol">The method to check</param>
		/// <returns>Is the return value immutable?</returns>
		public bool IsReturnValueKnownToBeImmutable( IMethodSymbol methodSymbol ) {
			return m_knownImmutableReturns.Contains( methodSymbol.OriginalDefinition );
		}

		public ImmutableTypeInfo GetImmutableTypeInfo(
			INamedTypeSymbol type
		) {
			// Check for [Immutable] etc.
			ImmutableTypeKind fromAttributes = GetImmutabilityFromAttributes( type );
			if( fromAttributes != ImmutableTypeKind.None ) {
				return ImmutableTypeInfo.Create(
					annotationsContext: m_annotationsContext,
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

			// Special-case for compile-time generated regexes
			if( IsGeneratedRegex( type ) ) {
				return ImmutableTypeInfo.Create(
					annotationsContext: m_annotationsContext,
					kind: ImmutableTypeKind.Total,
					type: type
				);
			}

			return ImmutableTypeInfo.Create(
				annotationsContext: m_annotationsContext,
				kind: ImmutableTypeKind.None,
				type: type
			);
		}

		private ImmutableTypeKind GetImmutabilityFromAttributes(
			ITypeSymbol type
		) {
			if ( m_annotationsContext.Objects.Immutable.IsDefined( type ) ) {
				return ImmutableTypeKind.Total;
			}

			if( m_annotationsContext.Objects.ConditionallyImmutable.IsDefined( type ) ) {
				return ImmutableTypeKind.Total;
			}

			if ( m_annotationsContext.Objects.ImmutableBaseClass.IsDefined( type ) ) {
				return ImmutableTypeKind.Instance;
			}

			return ImmutableTypeKind.None;
		}

		private bool IsGeneratedRegex( INamedTypeSymbol type ) {
			if( !m_regexInfo.RegexType.Equals( type.BaseType, SymbolEqualityComparer.Default ) ) {
				return false;
			}

			AttributeData a = type.GetAttributes().FirstOrDefault( a => m_regexInfo.GeneratedCodeAttributeType.Equals( a.AttributeClass, SymbolEqualityComparer.Default ) );
			if( a == null ) {
				return false;
			}

			string tool = (string)a.ConstructorArguments[ 0 ].Value;
			if( tool != "System.Text.RegularExpressions.Generator" ) {
				return false;
			}

			return true;
		}
	}
}
