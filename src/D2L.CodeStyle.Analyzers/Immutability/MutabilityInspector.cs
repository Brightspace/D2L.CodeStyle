using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[Flags]
	public enum MutabilityInspectionFlags {
		Default = 0,
		AllowUnsealed = 1,
		IgnoreImmutabilityAttribute = 2,
	}

	/// <summary>
	/// Determines what kind of immutable a type has.
	/// </summary>
	internal enum ImmutabilityScope {
		None,
		Self,
		SelfAndChildren
	}

	internal sealed class MutabilityInspector {

		private readonly KnownImmutableTypes m_knownImmutableTypes;
		private readonly Compilation m_compilation;

		private readonly ConcurrentDictionary<(ITypeSymbol Symbol, MutabilityInspectionFlags Flags), MutabilityInspectionResult> m_cache
			= new ConcurrentDictionary<(ITypeSymbol Symbol, MutabilityInspectionFlags Flags), MutabilityInspectionResult>();

		internal MutabilityInspector(
			Compilation compilation,
			KnownImmutableTypes knownImmutableTypes
		) {
			m_compilation = compilation;
			m_knownImmutableTypes = knownImmutableTypes;
		}

		/// <summary>
		/// Determine if a given type is mutable.
		/// </summary>
		/// <param name="type">The type to determine mutability for.</param>
		/// <returns>Whether the type is mutable.</returns>
		public MutabilityInspectionResult InspectType(
			ITypeSymbol type,
			MutabilityInspectionFlags flags = MutabilityInspectionFlags.Default
		) {
			var typesInCurrentCycle = new HashSet<ITypeSymbol>();

			return DoInspectType(
				type,
				flags,
				typesInCurrentCycle
			);
		}

		public MutabilityInspectionResult InspectConcreteType(
			ITypeSymbol type,
			MutabilityInspectionFlags flags = MutabilityInspectionFlags.Default
		) {
			var typesInCurrentCycle = new HashSet<ITypeSymbol>();

			return DoInspectConcreteType( type, typesInCurrentCycle, flags );
		}

		public MutabilityInspectionResult InspectMember( 
			ISymbol symbol
		) {
			var seen = new HashSet<ITypeSymbol>();

			return PerformMemberInspection( symbol, seen );
		}

		private MutabilityInspectionResult DoInspectType(
			ITypeSymbol type,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			return DoInspectType( type, default, flags, typeStack );
		}

		private MutabilityInspectionResult DoInspectType(
			ITypeSymbol type,
			ITypeSymbol hostSymbol,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			var cacheKey = (Symbol: type, Flags: flags);

			return m_cache.GetOrAdd(
				cacheKey,
				query => PerformTypeInspection( 
					query.Symbol, 
					hostSymbol,
					query.Flags, 
					typeStack )
			);
		}

		private MutabilityInspectionResult DoInspectConcreteType(
			ITypeSymbol type,
			HashSet<ITypeSymbol> typeStack,
			MutabilityInspectionFlags flags = MutabilityInspectionFlags.Default
		) {
			if( type is IErrorTypeSymbol ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// The concrete type System.Object is empty (and therefore safe.)
			// For example, `private readonly object m_lock = new object();` is fine. 
			if( type.SpecialType == SpecialType.System_Object ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// System.ValueType is the base class of all value types (obscure detail)
			if( type.SpecialType == SpecialType.System_ValueType ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// We need to *not* bail early if the type is generic otherwise
			// we will assume that anything marked with [Immutable] is
			// sufficiently immutable, and we also need to ensure the
			// immutability of the type parameters is also appropriate.
			ImmutabilityScope scope = type.GetImmutabilityScope();
			if( !type.IsGenericType()
				&& !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute )
				&& scope != ImmutabilityScope.None
			) {
				ImmutableHashSet<string> immutableExceptions = type.GetAllImmutableExceptions();
				return MutabilityInspectionResult.NotMutable( immutableExceptions );
			}

			return DoInspectType(
				type,
				flags | MutabilityInspectionFlags.AllowUnsealed,
				typeStack
			);
		}

		private MutabilityInspectionResult InspectImmutableContainerType(
			ITypeSymbol type,
			HashSet<ITypeSymbol> typeStack
		) {
			var namedType = type as INamedTypeSymbol;

			// If we can't determine what the symbol is then we'll bail with
			// a general mutability response, otherwise we're going to have a
			// lot of NREs below.
			if( namedType == default ) {
				return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsPotentiallyMutable );
			}

			ImmutableHashSet<string>.Builder unauditedReasonsBuilder = ImmutableHashSet.CreateBuilder<string>();

			for( int i = 0; i < namedType.TypeArguments.Length; i++ ) {
				ITypeSymbol arg = namedType.TypeArguments[i];

				MutabilityInspectionResult result = DoInspectType(
					arg,
					MutabilityInspectionFlags.Default,
					typeStack
				);

				if( result.IsMutable ) {
					if( result.Target == MutabilityTarget.Member ) {
						// modify the result to prefix with container member.
						string[] prefix = type.GetImmutableContainerTypePrefixes();
						result = result.WithPrefixedMember( prefix[i] );
					} else {
						// modify the result to target the type argument if the
						// target is not a member
						result = result.WithTarget(
							MutabilityTarget.TypeArgument
						);
					}
					return result;
				}

				unauditedReasonsBuilder.UnionWith( result.SeenUnauditedReasons );
			}

			return MutabilityInspectionResult.NotMutable( unauditedReasonsBuilder.ToImmutable() );
		}

		private MutabilityInspectionResult DoInspectInterface(
			ITypeSymbol symbol,
			HashSet<ITypeSymbol> typeStack
		) {
			var typeSymbol = symbol as INamedTypeSymbol;

			// If we can't determine what the symbol is then we'll bail with
			// a general mutability response, otherwise we're going to have a
			// lot of NREs below.
			if( typeSymbol == default ) {
				return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsPotentiallyMutable );
			}

			// There is a 1:1 correlation between TypeParameters and TypeArguments.
			// TypeParameters is the "S", or "T" definition.
			// TypeArguments are the actual *types* passed to S or T.
			for( int ordinal = 0; ordinal < typeSymbol.TypeParameters.Length; ordinal++ ) {

				bool isToBeImmutable = IsTypeArgumentImmutable(
					typeSymbol.TypeParameters[ordinal],
					ordinal,
					typeSymbol );

				if( !isToBeImmutable ) {
					continue;
				}

				ITypeSymbol parameterType = typeSymbol.TypeArguments[ordinal];
				MutabilityInspectionResult result = InspectType( parameterType );
				if( result.IsMutable ) {
					return MutabilityInspectionResult.MutableType(
						parameterType,
						MutabilityCause.IsMutableTypeParameter );
				}
			}

			// Detects if the interface itself is marked with Immutable
			ImmutabilityScope scope = typeSymbol.GetImmutabilityScope();
			if( scope != ImmutabilityScope.None ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// If we've reached here, it's an interface with no type parameters
			// that is not marked Immutable
			return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsAnInterface );
		}

		private MutabilityInspectionResult DoInspectTypeParameter(
			ITypeSymbol symbol,
			ITypeSymbol hostSymbol,
			HashSet<ITypeSymbol> typeStack
		) {
			var typeParameter = symbol as ITypeParameterSymbol;

			// If we can't determine what the symbol is then we'll bail with
			// a general mutability response, otherwise we're going to have a
			// lot of NREs below.
			if( typeParameter == default ) {
				return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsAGenericType );
			}

			if( typeParameter.ConstraintTypes != null ) {

				// there are constraints we can check. as type constraints are 
				// unionized, we only need one type constraint to be immutable 
				// to succeed
				foreach( ITypeSymbol constraintType in typeParameter.ConstraintTypes ) {

					MutabilityInspectionResult result = DoInspectType(
						constraintType,
						MutabilityInspectionFlags.Default,
						typeStack
					);

					if( !result.IsMutable ) {
						return result;
					}
				}
			}

			// We have to walk all base types and interfaces to find the
			// type param with the same name and examine those to see if
			// the immutability attribute is present.
			INamedTypeSymbol currentType = typeParameter.ContainingType;
			while( currentType != null ) {

				// Check all interfaces
				foreach( INamedTypeSymbol intf in currentType.Interfaces ) {

					// Find out if this interface exposes a type argument
					// with the specified type name.  If it doesn't, this 
					// interface doesn't contribute to the immutability chain.
					int ordinal = intf.IndexOfArgument( typeParameter.Name );
					if( ordinal < 0 ) {
						continue;
					}

					var subTypeSymbol = intf.TypeArguments[ordinal] as ITypeParameterSymbol;
					if( IsTypeArgumentImmutable( subTypeSymbol, ordinal, intf ) ) {
						return MutabilityInspectionResult.NotMutable();
					}
				}

				// Check the type
				if( typeParameter.GetImmutabilityScope() != ImmutabilityScope.None ) {
					return MutabilityInspectionResult.NotMutable();
				}

				// Walk up the type heirarchy
				currentType = currentType.BaseType;
			}

			return MutabilityInspectionResult.MutableType(
				symbol,
				MutabilityCause.IsAGenericType
			);
		}

		private MutabilityInspectionResult DoInspectInitializer(
			ExpressionSyntax expr,
			HashSet<ITypeSymbol> typeStack
		) {
			if( expr.Kind() == SyntaxKind.NullLiteralExpression ) {
				// This is perhaps a bit suspicious, because fields and
				// properties have to be readonly, but it is safe...
				return MutabilityInspectionResult.NotMutable();
			}

			SemanticModel model = m_compilation.GetSemanticModel( expr.SyntaxTree );

			TypeInfo typeInfo = model.GetTypeInfo( expr );

			// Type can be null in the case of an implicit conversion where
			// the expression alone doesn't have a type. For example:
			//   int[] foo = { 1, 2, 3 };
			ITypeSymbol exprType = typeInfo.Type ?? typeInfo.ConvertedType;

			if( expr is ObjectCreationExpressionSyntax ) {
				// If our initializer is "new Foo( ... )" we only need to
				// consider Foo concretely; not subtypes of Foo.
				return DoInspectConcreteType( exprType, typeStack );
			}

			// In general we can use the initializers type in place of the
			// field/properties type because it may be narrower.
			return DoInspectType(
				exprType,
				MutabilityInspectionFlags.Default,
				typeStack
			);
		}

		private MutabilityInspectionResult DoInspectProperty(
			IPropertySymbol property,
			ITypeSymbol hostSymbol,
			HashSet<ITypeSymbol> typeStack
		) {

			if( property.IsIndexer ) {
				// Indexer properties are just glorified method syntax and dont' hold state
				return MutabilityInspectionResult.NotMutable();
			}

			PropertyDeclarationSyntax propertySyntax =
				GetDeclarationSyntax<PropertyDeclarationSyntax>( property );

			// TODO: can we do this without the syntax; with only the symbol?
			if( !propertySyntax.IsAutoImplemented() ) {
				// Properties that are auto-implemented have an implicit
				// backing field that may be mutable. Otherwise, properties are
				// just sugar for getter/setter methods and don't themselves
				// contribute to mutability.
				return MutabilityInspectionResult.NotMutable();
			}

			if( !property.IsReadOnly ) {
				return MutabilityInspectionResult.MutableProperty(
					property,
					MutabilityCause.IsNotReadonly
				);
			}

			if( propertySyntax.Initializer != null ) {
				return DoInspectInitializer(
					propertySyntax.Initializer.Value,
					typeStack
				).WithPrefixedMember( property.Name );
			}

			return DoInspectType(
				property.Type,
				MutabilityInspectionFlags.Default,
				typeStack
			).WithPrefixedMember( property.Name );
		}

		private MutabilityInspectionResult DoInspectField(
			IFieldSymbol field,
			ITypeSymbol hostSymbol,
			HashSet<ITypeSymbol> typeStack
		) {
			if( !field.IsReadOnly ) {
				return MutabilityInspectionResult.MutableField(
					field,
					MutabilityCause.IsNotReadonly
				);
			}

			VariableDeclaratorSyntax declSyntax =
				GetDeclarationSyntax<VariableDeclaratorSyntax>( field );

			if( declSyntax.Initializer != null ) {
				return DoInspectInitializer(
					declSyntax.Initializer.Value,
					typeStack
				).WithPrefixedMember( field.Name );
			}

			return DoInspectType(
				field.Type,
				MutabilityInspectionFlags.Default,
				typeStack
			).WithPrefixedMember( field.Name );
		}

		/// <summary>
		/// Walks all ancestors to this type parameter to determine if it was
		/// intended that the type be immutable.  If *any* ancestor says the
		/// type was supposed to be immutable, then the type will be 
		/// assumed to mandatory immutable.
		/// </summary>
		private static bool IsTypeArgumentImmutable(
			ITypeParameterSymbol typeParameter,
			int parameterOrdinal,
			INamedTypeSymbol symbol
		) {
			if( symbol == default ) {
				return false;
			}

			if( typeParameter == default ) {
				return false;
			}

			// We need the TypeParameter here, otherwise we're inspecting
			// the type and not the declaration.  We need to inspect the 
			// declaration because that's the symbol that will have the
			// [Immutable] attached to it.
			bool isMarkedImmutable = symbol.TypeParameters[parameterOrdinal]
				.GetImmutabilityScope() != ImmutabilityScope.None;

			if( isMarkedImmutable ) {
				return true;
			}

			foreach( INamedTypeSymbol intf in symbol.Interfaces ) {

				int ordinal = intf.IndexOfArgument( typeParameter.Name );
				if( ordinal < 0 ) {
					continue;
				}

				// We pass through the type argument otherwise the "name"
				// applied to the type will drift based on the declaration
				// and be impossible to track.  Using this means that the
				// name declared at the top is consistent all the way down.
				var subTypeParameter = intf.TypeArguments[ordinal] as ITypeParameterSymbol;
				if( IsTypeArgumentImmutable( subTypeParameter, ordinal, intf ) ) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Get the declaration syntax for a symbol. This is intended to be
		/// used for fields and properties which can't have multiple
		/// declaration nodes.
		/// </summary>
		private static T GetDeclarationSyntax<T>( ISymbol symbol )
			where T : SyntaxNode {
			ImmutableArray<SyntaxReference> decls = symbol.DeclaringSyntaxReferences;

			if( decls.Length != 1 ) {
				throw new NotImplementedException(
					"Unexepected number of decls: "
					+ decls.Length
				);
			}

			SyntaxNode syntax = decls[0].GetSyntax();

			var decl = syntax as T;
			if( decl == null ) {

				string msg = String.Format(
						"Couldn't cast declaration syntax of type '{0}' as type '{1}': {2}",
						syntax.GetType().FullName,
						typeof( T ).FullName,
						symbol.ToDisplayString()
					);

				throw new InvalidOperationException( msg );
			}

			return decl;
		}

		private MutabilityInspectionResult PerformMemberInspection(
			ISymbol symbol,
			HashSet<ITypeSymbol> typeStack
		) {
			return PerformMemberInspection( symbol, default, typeStack );
		}

		private MutabilityInspectionResult PerformMemberInspection(
			ISymbol symbol,
			ITypeSymbol hostSymbol,
			HashSet<ITypeSymbol> typeStack
		) {
			// if the member is audited or unaudited, ignore it
			if( Attributes.Mutability.Audited.IsDefined( symbol ) ) {
				return MutabilityInspectionResult.NotMutable();
			}
			if( Attributes.Mutability.Unaudited.IsDefined( symbol ) ) {
				string unauditedReason = BecauseHelpers.GetUnauditedReason( symbol );
				return MutabilityInspectionResult.NotMutable( ImmutableHashSet.Create( unauditedReason ) );
			}

			switch( symbol.Kind ) {
				case SymbolKind.Property:
					return DoInspectProperty(
						symbol as IPropertySymbol,
						hostSymbol,
						typeStack
					);

				case SymbolKind.Field:
					return DoInspectField(
						symbol as IFieldSymbol,
						hostSymbol,
						typeStack
					);

				case SymbolKind.Method:
				case SymbolKind.NamedType:
					// ignore these symbols, because they do not contribute to immutability
					return MutabilityInspectionResult.NotMutable();

				default:
					// we've got a member (event, etc.) that we can't currently be smart about, so fail
					return MutabilityInspectionResult.PotentiallyMutableMember( symbol );
			}
		}

		private MutabilityInspectionResult PerformTypeInspection(
			ITypeSymbol type,
			ITypeSymbol hostSymbol,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			if( type is IErrorTypeSymbol ) {
				// This only happens for code that otherwise won't compile. Our
				// analyzer doesn't need to validate these types. It only needs
				// to be strict for valid code.
				return MutabilityInspectionResult.NotMutable();
			}

			if( type == null ) {
				throw new Exception( "Type cannot be resolved. Please ensure all dependencies "
					+ "are referenced, including transitive dependencies." );
			}

			// If we're not verifying immutability, we might be able to bail 
			// out early, but we will not exit early if the type is simply 
			// marked immutable if the type is generic since we need to 
			// actually examine the generic type parameters.
			ImmutabilityScope scope = type.GetImmutabilityScope();
			if( !type.IsGenericType()
				&& !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute )
				&& scope == ImmutabilityScope.SelfAndChildren
			) {
				ImmutableHashSet<string> immutableExceptions = type.GetAllImmutableExceptions();
				return MutabilityInspectionResult.NotMutable( immutableExceptions );
			}

			if( m_knownImmutableTypes.IsTypeKnownImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( type.IsAnImmutableContainerType() ) {
				return InspectImmutableContainerType( type, typeStack );
			}

			if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
					&& type.TypeKind == TypeKind.Class
					&& !type.IsSealed
				) {
				return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsNotSealed );
			}

			switch( type.TypeKind ) {
				case TypeKind.Array:
					// Arrays are always mutable because you can rebind the
					// individual elements.
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsAnArray
					);

				case TypeKind.Delegate:
					// Delegates can hold state so are mutable in general.
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsADelegate
					);

				case TypeKind.Dynamic:
					// Dynamic types are always mutable
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsDynamic
					);

				case TypeKind.Enum:
					// Enums are just fancy ints.
					return MutabilityInspectionResult.NotMutable();

				case TypeKind.TypeParameter:
					return DoInspectTypeParameter(
						type,
						hostSymbol,
						typeStack
					);

				case TypeKind.Interface:
					return DoInspectInterface(
						type,
						typeStack
					);

				case TypeKind.Class:
				case TypeKind.Struct: // equivalent to TypeKind.Structure
					return InspectClassOrStruct(
						type,
						typeStack
					);

				case TypeKind.Error:
					// This only happens when the build is failing for other
					// (more fundamental) reasons. We only need to be strict
					// for otherwise-successful builds, so we bail analysis in
					// this case.
					return MutabilityInspectionResult.NotMutable();

				case TypeKind.Unknown:
					// Looking at the Roslyn source this doesn't appear to
					// happen outside their tests. It is value 0 in the enum so
					// it may just be a safety guard.
					throw new NotImplementedException();

				default:
					// not handled: Module, Pointer, Submission.
					throw new NotImplementedException(
						$"TypeKind.{type.Kind} not handled by analysis"
					);
			}
		}

		private MutabilityInspectionResult InspectClassOrStruct(
			ITypeSymbol type,
			HashSet<ITypeSymbol> typeStack
		) {
			if( typeStack.Contains( type ) ) {
				// We have a cycle. If we're here, it means that either some read-only member causes the cycle (via IsMemberMutableRecursive), 
				// or a generic parameter to a type causes the cycle (via IsTypeMutableRecursive). This is safe if the checks above have 
				// passed and the remaining members are read-only immutable. So we can skip the current check, and allow the type to continue 
				// to be evaluated.
				return MutabilityInspectionResult.NotMutable();
			}

			// We have a type that is not marked immutable, is not an interface, is not an immutable container, etc..
			// If it is defined in a different assembly, we might not have the metadata to correctly analyze it; so we fail.
			if( type.IsFromOtherAssembly( m_compilation ) ) {
				return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnExternalUnmarkedType );
			}

			ImmutableHashSet<string>.Builder seenUnauditedReasonsBuilder = ImmutableHashSet.CreateBuilder<string>();

			typeStack.Add( type );
			try {
				foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
					MutabilityInspectionResult result = PerformMemberInspection( member, type, typeStack );
					if( result.IsMutable ) {
						return result;
					}
					seenUnauditedReasonsBuilder.UnionWith( result.SeenUnauditedReasons );
				}

				// We descend into the base class last
				if( type.BaseType != null ) {
					MutabilityInspectionResult baseResult = DoInspectConcreteType( type.BaseType, typeStack );

					if( baseResult.IsMutable ) {
						return baseResult;
					}
					seenUnauditedReasonsBuilder.UnionWith( baseResult.SeenUnauditedReasons );
				}
			} finally {
				typeStack.Remove( type );
			}

			return MutabilityInspectionResult.NotMutable( seenUnauditedReasonsBuilder.ToImmutable() );
		}

	}
}