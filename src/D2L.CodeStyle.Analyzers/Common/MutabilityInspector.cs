using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace D2L.CodeStyle.Analyzers.Common {

	[Flags]
	public enum MutabilityInspectionFlags {
		Default = 0,
		AllowUnsealed = 1,
		IgnoreImmutabilityAttribute = 2,
	}

	internal sealed class MutabilityInspector {

		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = new HashSet<string> {
			"System.StringComparer"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of immutable container types (i.e., types that hold other types)
		/// </summary>
		private static readonly ImmutableDictionary<string, string[]> ImmutableContainerTypes = new Dictionary<string, string[]> {
			[ "D2L.LP.Utilities.DeferredInitializer" ] = new[] { "Value" },
			[ "System.Collections.Immutable.ImmutableArray" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Immutable.ImmutableHashSet" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableList" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableQueue" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableSortedDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Immutable.ImmutableSortedSet" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableStack" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyCollection" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyList" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Generic.IEnumerable" ] = new[] { "[]" },
			[ "System.Lazy" ] = new[] { "Value" },
			[ "System.Nullable" ] = new[] { "Value" },
			[ "System.Tuple" ] = new[] { "Item1", "Item2", "Item3", "Item4", "Item5", "Item6" }
		}.ToImmutableDictionary();

		private readonly KnownImmutableTypes _knownImmutableTypes;

		internal MutabilityInspector(
			KnownImmutableTypes knownImmutableTypes
		) {
			_knownImmutableTypes = knownImmutableTypes;
		}

		/// <summary>
		/// A list of <see cref="TypeKind"/>s that are unsafe.
		/// </summary>
		private static readonly ImmutableDictionary<TypeKind, MutabilityCause> UnsafeTypeKinds = new Dictionary<TypeKind, MutabilityCause> {
			[TypeKind.Array] = MutabilityCause.IsAnArray,
			[TypeKind.Delegate] = MutabilityCause.IsADelegate,
			[TypeKind.Dynamic] = MutabilityCause.IsDynamic
		}.ToImmutableDictionary();

		/// <summary>
		/// A list of <see cref="TypeKind"/> that are immutable.
		/// </summary>
		private static readonly ImmutableHashSet<TypeKind> ImmutableTypeKinds = new HashSet<TypeKind> {
			TypeKind.Enum
		}.ToImmutableHashSet();

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
			var result = InspectTypeRecursive( type, flags, typesInCurrentCycle );
			return result;
		}

		private MutabilityInspectionResult InspectTypeRecursive(
			ITypeSymbol type,
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

			if( _knownImmutableTypes.IsTypeKnownImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( UnsafeTypeKinds.ContainsKey( type.TypeKind ) ) {
				var cause = UnsafeTypeKinds[type.TypeKind];
				return MutabilityInspectionResult.MutableType( type, cause );
			}

			if( ImmutableTypeKinds.Contains( type.TypeKind ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// If we're verifying immutability, then carry on; otherwise, bailout
			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute ) && IsTypeMarkedImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( typeStack.Contains( type ) ) {
				// We have a cycle. If we're here, it means that either some read-only member causes the cycle (via IsMemberMutableRecursive), 
				// or a generic parameter to a type causes the cycle (via IsTypeMutableRecursive). This is safe if the checks above have 
				// passed and the remaining members are read-only immutable. So we can skip the current check, and allow the type to continue 
				// to be evaluated.
				return MutabilityInspectionResult.NotMutable();
			}

			typeStack.Add( type );
			try {

				if( ImmutableContainerTypes.ContainsKey( type.GetFullTypeName() ) ) {
					var namedType = type as INamedTypeSymbol;
					for( int i = 0; i < namedType.TypeArguments.Length; i++ ) {
						var arg = namedType.TypeArguments[ i ];
						var result = InspectTypeRecursive( arg, MutabilityInspectionFlags.Default, typeStack );

						if( result.IsMutable ) {
							if( result.Target == MutabilityTarget.Member ) {

								// modify the result to prefix with container member.
								var prefix = ImmutableContainerTypes[ type.GetFullTypeName() ];
								result = result.WithPrefixedMember( prefix[i] );

							} else {

								// modify the result to target the type argument if the target is not a member
								result = result.WithTarget( MutabilityTarget.TypeArgument );

							}
							return result;
						}
					}
					return MutabilityInspectionResult.NotMutable();
				}

				if( type.TypeKind == TypeKind.Interface ) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnInterface );
				}

				if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
						&& type.TypeKind == TypeKind.Class
						&& !type.IsSealed
					) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsNotSealed );
				}

				foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
					var result = InspectMemberRecursive( member, MutabilityInspectionFlags.Default, typeStack );
					if( result.IsMutable ) {
						return result;
					}
				}

				return MutabilityInspectionResult.NotMutable();

			} finally {
				typeStack.Remove( type );
			}
		}

		private MutabilityInspectionResult InspectMemberRecursive(
			ISymbol symbol,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {

			MutabilityInspectionResult result;

			switch( symbol.Kind ) {

				case SymbolKind.Property:

					var prop = (IPropertySymbol)symbol;

					var sourceTree = prop.DeclaringSyntaxReferences.FirstOrDefault();
					var declarationSyntax = sourceTree?.GetSyntax() as PropertyDeclarationSyntax;
					if( declarationSyntax != null && !declarationSyntax.IsAutoImplemented() ) {
						// non-auto properties never hold state themselves
						return MutabilityInspectionResult.NotMutable();
					}

					if( !prop.IsReadOnly ) {
						return MutabilityInspectionResult.MutableProperty( prop, MutabilityCause.IsNotReadonly );
					}

					result = InspectTypeRecursive( prop.Type, flags, typeStack );
					if( result.IsMutable ) {
						return result.WithPrefixedMember( prop.Name );
					}

					return MutabilityInspectionResult.NotMutable();

				case SymbolKind.Field:

					var field = (IFieldSymbol)symbol;

					if( !field.IsReadOnly ) {
						return MutabilityInspectionResult.MutableField( field, MutabilityCause.IsNotReadonly );
					}

					result = InspectTypeRecursive( field.Type, flags, typeStack );
					if( result.IsMutable ) {
						return result.WithPrefixedMember( field.Name );
					}

					return MutabilityInspectionResult.NotMutable();

				case SymbolKind.Method:
				case SymbolKind.NamedType:
					// ignore these symbols, because they do not contribute to immutability
					return MutabilityInspectionResult.NotMutable();

				default:
					// we've got a member (event, etc.) that we can't currently be smart about, so fail
					return MutabilityInspectionResult.PotentiallyMutableMember( symbol );
			}
		}

		public bool IsTypeMarkedImmutable( ITypeSymbol symbol ) {
			if( MarkedImmutableTypes.Contains( symbol.GetFullTypeName() ) ) {
				return true;
			}
			if( symbol.GetAttributes().Any( a => a.AttributeClass.Name == "Immutable" ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedImmutable ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedImmutable( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}
	}
}
