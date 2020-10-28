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

		internal MutabilityInspector( Compilation compilation ) {
			m_compilation = compilation;
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

			return DoInspectConcreteType(
				type,
				typesInCurrentCycle,
				flags );
		}

		public MutabilityInspectionResult InspectMember(
			ISymbol symbol
		) {
			var typesInCurrentCycle = new HashSet<ITypeSymbol>();

			return PerformMemberInspection(
				symbol,
				typesInCurrentCycle );
		}

		private MutabilityInspectionResult DoInspectType(
			ITypeSymbol type,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			return DoInspectType(
				type,
				default,
				flags,
				typeStack );
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
					type: query.Symbol,
					hostSymbol: hostSymbol,
					flags: query.Flags,
					typeStack: typeStack )
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

			ImmutabilityScope scope = type.GetImmutabilityScope();
			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute )
				&& scope != ImmutabilityScope.None
			) {
				return MutabilityInspectionResult.NotMutable();
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

			for( int i = 0; i < namedType.TypeArguments.Length; i++ ) {
				ITypeSymbol arg = namedType.TypeArguments[i];

				MutabilityInspectionResult result = DoInspectType(
					type: arg,
					hostSymbol: type,
					flags: MutabilityInspectionFlags.Default,
					typeStack: typeStack
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
			}

			return MutabilityInspectionResult.NotMutable();
		}

		private MutabilityInspectionResult DoInspectInterface(
			ITypeSymbol symbol,
			ITypeSymbol hostSymbol,
			HashSet<ITypeSymbol> typeStack
		) {
			var typeSymbol = symbol as INamedTypeSymbol;

			// If we can't determine what the symbol is then we'll bail with
			// a general mutability response, otherwise we're going to have a
			// lot of NREs below.
			if( typeSymbol == default ) {
				return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsPotentiallyMutable );
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

			var hostType = hostSymbol as INamedTypeSymbol;

			if( hostType == default ) {
				// As a generic type, this case should not be possible, but to
				// ensure the analysis completes without explosion we will
				// just mark the type as mutable for a fail-safe fallback.
				return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsPotentiallyMutable );
			}

			int ordinal = hostType.IndexOfArgument( typeParameter.Name );
			if( ordinal < 0 ) {
				// We're examing a T inside a Foo<T>, this shouldn't be possible
				// but again, we'll return the type is potentially mutable to
				// ensure the analysis won't allow something bad, but also
				// doesn't explode.
				return MutabilityInspectionResult.MutableType( symbol, MutabilityCause.IsPotentiallyMutable );
			}

			ImmutabilityScope argumentScope =
				hostType.TypeArguments[ordinal].GetImmutabilityScope();

			if( argumentScope == ImmutabilityScope.SelfAndChildren ) {
				// We can assume T is immutable since it's marked for
				// immutability in the class declaration
				return MutabilityInspectionResult.NotMutable();
			}

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
						type: constraintType,
						hostSymbol: hostSymbol,
						flags: MutabilityInspectionFlags.Default,
						typeStack: typeStack
					);

					if( !result.IsMutable ) {
						return result;
					}
				}
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

			switch( expr.Kind() ) {
				// This is perhaps a bit suspicious, because fields and
				// properties have to be readonly, but it is safe...
				case SyntaxKind.NullLiteralExpression:

				// Lambda initializers for readonly members are safe
				// because they can only close over other members, which
				// will be checked independently, or static members of
				// another class, which are also analyzed
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
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
				property.GetDeclarationSyntax<PropertyDeclarationSyntax>();

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
				type: property.Type,
				hostSymbol: hostSymbol,
				flags: MutabilityInspectionFlags.Default,
				typeStack: typeStack
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
				field.GetDeclarationSyntax<VariableDeclaratorSyntax>();

			if( declSyntax.Initializer != null ) {
				return DoInspectInitializer(
					declSyntax.Initializer.Value,
					typeStack
				).WithPrefixedMember( field.Name );
			}

			return DoInspectType(
				type: field.Type,
				hostSymbol: hostSymbol,
				flags: MutabilityInspectionFlags.Default,
				typeStack: typeStack
			).WithPrefixedMember( field.Name );
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
						property: symbol as IPropertySymbol,
						hostSymbol: hostSymbol,
						typeStack: typeStack
					);

				case SymbolKind.Field:
					return DoInspectField(
						field: symbol as IFieldSymbol,
						hostSymbol: hostSymbol,
						typeStack: typeStack
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

			ImmutabilityScope scope = type.GetImmutabilityScope();
			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute )
				&& scope == ImmutabilityScope.SelfAndChildren
			) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( KnownImmutableTypes.IsTypeKnownImmutable( type ) ) {
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

				case TypeKind.TypeParameter:
					return DoInspectTypeParameter(
						type,
						hostSymbol,
						typeStack
					);

				case TypeKind.Interface:
					return DoInspectInterface(
						type,
						hostSymbol,
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

			typeStack.Add( type );
			try {
				foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
					MutabilityInspectionResult result = PerformMemberInspection( member, type, typeStack );
					if( result.IsMutable ) {
						return result;
					}
				}

				// We descend into the base class last
				if( type.BaseType != null ) {
					MutabilityInspectionResult baseResult = DoInspectConcreteType( type.BaseType, typeStack );

					if( baseResult.IsMutable ) {
						return baseResult;
					}
				}
			} finally {
				typeStack.Remove( type );
			}

			return MutabilityInspectionResult.NotMutable();
		}
	}
}
