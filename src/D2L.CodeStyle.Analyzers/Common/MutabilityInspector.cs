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

	public sealed class MutabilityInspector {

		/// <summary>
		/// A list of types that are not allowed to be used in immutable types. 
		/// 
		/// The reason is because their internals often hold onto state that we
		/// cannot analyze.
		/// </summary>
		private static readonly ImmutableHashSet<string> DisallowedTypes = new HashSet<string> {
			"System.Action",
			"System.Func"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of known immutable types
		/// </summary>
		private static readonly ImmutableHashSet<string> KnownImmutableTypes = new HashSet<string> {
			"count4net.IRateCounter",
			"count4net.IStatCounter",
			"log4net.ILog",
			"Newtonsoft.Json.JsonSerializer",
			"System.ComponentModel.TypeConverter",
			"System.DateTime",
			"System.Drawing.Size", // only safe because it's a struct with primitive fields
			"System.Guid",
			"System.Reflection.ConstructorInfo",
			"System.Reflection.FieldInfo",
			"System.Reflection.MemberInfo",
			"System.Reflection.MethodInfo",
			"System.Reflection.PropertyInfo",
			"System.Text.RegularExpressions.Regex",
			"System.TimeSpan",
			"System.Type",
			"System.Uri",
			"System.String",
			"System.Version",
			"System.Workflow.ComponentModel.DependencyProperty",
			"System.Xml.Serialization.XmlSerializer"
		}.ToImmutableHashSet();

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
			if( type is IErrorTypeSymbol || type == null ) {
				throw new Exception( $"Type '{type}' cannot be resolved. Please ensure all dependencies "
					+ "are referenced, including transitive dependencies." );
			}

			if( IsTypeKnownImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( type.TypeKind == TypeKind.Array ) {
				return MutabilityInspectionResult.Mutable( null, type.GetFullTypeName(), MutabilityTarget.Type, MutabilityCause.IsAnArray );
			}

			if( !IsTypeAllowed( type ) ) {
				return MutabilityInspectionResult.Mutable( null, type.GetFullTypeName(), MutabilityTarget.Type, MutabilityCause.IsNotAllowed );
			}

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
								result = MutabilityInspectionResult.Mutable( 
									result.MemberPath, 
									result.TypeName, 
									MutabilityTarget.TypeArgument, 
									result.Cause.Value 
								);

							}
							return result;
						}
					}
					return MutabilityInspectionResult.NotMutable();
				}

				if( type.TypeKind == TypeKind.Interface ) {
					return MutabilityInspectionResult.Mutable( null, type.GetFullTypeName(), MutabilityTarget.Type, MutabilityCause.IsAnInterface );
				}

				if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
						&& type.TypeKind == TypeKind.Class
						&& !type.IsSealed
					) {
					return MutabilityInspectionResult.Mutable( null, type.GetFullTypeName(), MutabilityTarget.Type, MutabilityCause.IsNotSealed );
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
					if( declarationSyntax != null && declarationSyntax.IsPropertyGetterImplemented() ) {
						// property has getter with body; it is either backed by a field, or is a static function; ignore
						return MutabilityInspectionResult.NotMutable();
					}

					if( IsPropertyMutable( prop ) ) {
						return MutabilityInspectionResult.Mutable( prop.Name, prop.Type.GetFullTypeName(), MutabilityTarget.Member, MutabilityCause.IsNotReadonly );
					}

					result = InspectTypeRecursive( prop.Type, flags, typeStack );
					if( result.IsMutable ) {
						return result.WithPrefixedMember( prop.Name );
					}

					return MutabilityInspectionResult.NotMutable();

				case SymbolKind.Field:

					var field = (IFieldSymbol)symbol;

					if( IsFieldMutable( field ) ) {
						return MutabilityInspectionResult.Mutable( field.Name, field.Type.GetFullTypeName(), MutabilityTarget.Member, MutabilityCause.IsNotReadonly );
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
					return MutabilityInspectionResult.Mutable( symbol.Name, null, MutabilityTarget.Member, MutabilityCause.IsPotentiallyMutable );
			}
		}

		private bool IsTypeAllowed( ITypeSymbol type ) {
			if( DisallowedTypes.Contains( type.GetFullTypeName() ) ) {
				return false;
			}
			return true;
		}

		private bool IsTypeKnownImmutable( ITypeSymbol type ) {
			if( type.IsPrimitive() ) {
				return true;
			}

			if( KnownImmutableTypes.Contains( type.GetFullTypeName() ) ) {
				return true;
			}

			return false;
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

		/// <summary>
		/// Determine if a property is mutable.
		/// This does not check if the type of the property is also mutable; use <see cref="InspectType"/> for that.
		/// </summary>
		/// <param name="prop">The property to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsPropertyMutable( IPropertySymbol prop ) {
			if( prop.IsReadOnly ) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if a field is mutable.
		/// This does not check if the type of the field is also mutable; use <see cref="InspectType"/> for that.
		/// </summary>
		/// <param name="field">The field to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsFieldMutable( IFieldSymbol field ) {
			if( field.IsReadOnly ) {
				return false;
			}
			return true;
		}

	}
}
