using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.UTF8Encoding",
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

		private readonly KnownImmutableTypes m_knownImmutableTypes;
		private readonly Compilation m_compilation;

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
			var result = InspectTypeRecursive(
				type,
				flags,
				typesInCurrentCycle
			);
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

			// If we're verifying immutability, then carry on; otherwise, bailout
			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute ) && IsTypeMarkedImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// System.Object is safe if we can allow unsealed types (i.e., the type is the concrete type). 
			// For example, `private readonly object m_lock = new object();` is fine. 
			// There is no state, so we finish early.
			if( flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed ) && type.SpecialType == SpecialType.System_Object ) {
				return MutabilityInspectionResult.NotMutable();
			}

			// System.ValueType is the base class of all value types (obscure detail)
			if( flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed ) && type.SpecialType == SpecialType.System_ValueType ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( m_knownImmutableTypes.IsTypeKnownImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( IsAnImmutableContainerType( type ) ) {
				return InspectImmutableContainerType( type, typeStack );
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
					if ( flags != MutabilityInspectionFlags.Default ) {
						// remove this once we no longer take flags
						throw new InvalidOperationException( "This shouldn't happen" );
					}

					return InspectTypeParameter(
						type,
						typeStack
					);

				case TypeKind.Interface:
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnInterface );

				case TypeKind.Class:
				case TypeKind.Struct: // equivalent to TypeKind.Structure
					return InspectClassOrStruct(
						type,
						flags,
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

		private MutabilityInspectionResult InspectBaseType(
			ITypeSymbol type,
			HashSet<ITypeSymbol> typeStack
		) {
			if( type.BaseType == null ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( type.BaseType is IErrorTypeSymbol ) {
				return MutabilityInspectionResult.NotMutable();
			}

			return InspectTypeRecursive(
				type.BaseType,
				MutabilityInspectionFlags.AllowUnsealed,
				typeStack
			);
		}

		private MutabilityInspectionResult InspectClassOrStruct(
			ITypeSymbol type,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			if( typeStack.Contains( type ) ) {
				// We have a cycle. If we're here, it means that either some read-only member causes the cycle (via IsMemberMutableRecursive), 
				// or a generic parameter to a type causes the cycle (via IsTypeMutableRecursive). This is safe if the checks above have 
				// passed and the remaining members are read-only immutable. So we can skip the current check, and allow the type to continue 
				// to be evaluated.
				return MutabilityInspectionResult.NotMutable();
			}

			typeStack.Add( type );
			try {
				if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
						&& type.TypeKind == TypeKind.Class
						&& !type.IsSealed
					) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsNotSealed );
				}

				// We have a type that is not marked immutable, is not an interface, is not an immutable container, etc..
				// If it is defined in a different assembly, we might not have the metadata to correctly analyze it; so we fail.
				if( type.ContainingAssembly != m_compilation.Assembly ) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnExternalUnmarkedType );
				}

				foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
					var result = InspectMemberRecursive( member, typeStack );
					if( result.IsMutable ) {
						return result;
					}
				}

				// We descend into the base class last
				var baseResult = InspectBaseType( type, typeStack );
				if( baseResult.IsMutable ) {
					return baseResult;
				}

				return MutabilityInspectionResult.NotMutable();

			} finally {
				typeStack.Remove( type );
			}
		}

		private bool IsAnImmutableContainerType( ITypeSymbol type ) {
			return ImmutableContainerTypes.ContainsKey( type.GetFullTypeName() );
		}

		private MutabilityInspectionResult InspectImmutableContainerType(
			ITypeSymbol type,
			HashSet<ITypeSymbol> typeStack
		) {
			var namedType = type as INamedTypeSymbol;

			for( int i = 0; i < namedType.TypeArguments.Length; i++ ) {
				var arg = namedType.TypeArguments[ i ];

				var result = InspectTypeRecursive(
					arg,
					MutabilityInspectionFlags.Default,
					typeStack
				);

				if( result.IsMutable ) {
					if( result.Target == MutabilityTarget.Member ) {
						// modify the result to prefix with container member.
						var prefix = ImmutableContainerTypes[type.GetFullTypeName()];
						result = result.WithPrefixedMember( prefix[ i ] );
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

		private MutabilityInspectionResult InspectTypeParameter(
			ITypeSymbol symbol,
			HashSet<ITypeSymbol> typeStack
		) {
			var typeParameter = symbol as ITypeParameterSymbol;

			if( typeParameter.ConstraintTypes != null || typeParameter.ConstraintTypes.Length > 0 ) {
				// there are constraints we can check. as type constraints are unionized, we only need one 
				// type constraint to be immutable to succeed
				foreach( var constraintType in typeParameter.ConstraintTypes ) {

					var result = InspectTypeRecursive(
						constraintType,
						MutabilityInspectionFlags.Default,
						typeStack
					);

					if( !result.IsMutable ) {
						return MutabilityInspectionResult.NotMutable();
					}
				}
			}

			return MutabilityInspectionResult.MutableType(
				symbol,
				MutabilityCause.IsAGenericType
			);
		}

		private MutabilityInspectionResult InspectInitializer(
			ExpressionSyntax expr,
			HashSet<ITypeSymbol> typeStack
		) {
			if( expr.Kind() == SyntaxKind.NullLiteralExpression ) {
				// This is perhaps a bit suspicious, because fields and
				// properties have to be readonly, but it is safe...
				return MutabilityInspectionResult.NotMutable();
			}

			var model = m_compilation.GetSemanticModel( expr.SyntaxTree );

			var typeInfo = model.GetTypeInfo( expr );

			// Type can be null in the case of an implicit conversion where
			// the expression alone doesn't have a type. For example:
			//   int[] foo = { 1, 2, 3 };
			var exprType = typeInfo.Type ?? typeInfo.ConvertedType;

			if( expr is ObjectCreationExpressionSyntax ) {
				// If our initializer is "new Foo( ... )" we only need to
				// consider Foo concretely; not subtypes of Foo.
				return InspectTypeRecursive(
					exprType,
					MutabilityInspectionFlags.AllowUnsealed,
					typeStack
				);
			}

			// In general we can use the initializers type in place of the
			// field/properties type because it may be narrower.
			return InspectTypeRecursive(
				exprType,
				MutabilityInspectionFlags.Default,
				typeStack
			);
		}

		private MutabilityInspectionResult InspectProperty(
			IPropertySymbol property,
			HashSet<ITypeSymbol> typeStack
		) {

			if( property.IsIndexer ) {
				// Indexer properties are just glorified method syntax and dont' hold state
				return MutabilityInspectionResult.NotMutable();
			}

			var propertySyntax =
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
				return InspectInitializer(
					propertySyntax.Initializer.Value,
					typeStack
				).WithPrefixedMember( property.Name );
			}

			return InspectTypeRecursive(
				property.Type,
				MutabilityInspectionFlags.Default,
				typeStack
			).WithPrefixedMember( property.Name );
		}

		private MutabilityInspectionResult InspectField(
			IFieldSymbol field,
			HashSet<ITypeSymbol> typeStack
		) {
			if( !field.IsReadOnly ) {
				return MutabilityInspectionResult.MutableField(
					field,
					MutabilityCause.IsNotReadonly
				);
			}

			var declSyntax =
				GetDeclarationSyntax<VariableDeclaratorSyntax>( field );

			if( declSyntax.Initializer != null ) {
				return InspectInitializer(
					declSyntax.Initializer.Value,
					typeStack
				).WithPrefixedMember( field.Name );
			}

			return InspectTypeRecursive(
				field.Type,
				MutabilityInspectionFlags.Default,
				typeStack
			).WithPrefixedMember( field.Name );
		}

		/// <summary>
		/// Get the declaration syntax for a symbol. This is intended to be
		/// used for fields and properties which can't have multiple
		/// declaration nodes.
		/// </summary>
		private static T GetDeclarationSyntax<T>( ISymbol symbol )
			where T : SyntaxNode {
			var decls = symbol.DeclaringSyntaxReferences;

			if( decls.Length != 1 ) {
				throw new NotImplementedException(
					"Unexepected number of decls: "
					+ decls.Length
				);
			}

			SyntaxNode syntax = decls[ 0 ].GetSyntax();

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

		private MutabilityInspectionResult InspectMemberRecursive(
			ISymbol symbol,
			HashSet<ITypeSymbol> typeStack
		) {
			// if the member is audited or unaudited, ignore it
			if( Attributes.Mutability.Audited.IsDefined( symbol ) ) {
				return MutabilityInspectionResult.NotMutable();
			}
			if( Attributes.Mutability.Unaudited.IsDefined( symbol ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			switch( symbol.Kind ) {
				case SymbolKind.Property:
					return InspectProperty(
						symbol as IPropertySymbol,
						typeStack
					);

				case SymbolKind.Field:
					return InspectField(
						symbol as IFieldSymbol,
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

		public bool IsTypeMarkedImmutable( ITypeSymbol symbol ) {
			if( MarkedImmutableTypes.Contains( symbol.GetFullTypeName() ) ) {
				return true;
			}
			if( Attributes.Objects.Immutable.IsDefined( symbol ) ) {
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

		public bool IsTypeMarkedSingleton( ITypeSymbol symbol ) {
			if( Attributes.Singleton.IsDefined( symbol ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedSingleton ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedSingleton( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}
	}
}
