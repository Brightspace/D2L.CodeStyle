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
		/// A list of known immutable types
		/// </summary>
		private static readonly ImmutableHashSet<string> KnownImmutableTypes = new HashSet<string> {
			"Newtonsoft.Json.JsonSerializer",
			"System.ComponentModel.TypeConverter",
			"System.DateTime",
			"System.Guid",
			"System.Lazy",
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
		/// A list of immutable collections types (i.e., safe collection types)
		/// </summary>
		private static readonly ImmutableHashSet<string> ImmutableContainerTypes = new HashSet<string> {
			"D2L.LP.Utilities.DeferredInitializer",
			"System.Collections.Immutable.ImmutableArray",
			"System.Collections.Immutable.ImmutableDictionary",
			"System.Collections.Immutable.ImmutableHashSet",
			"System.Collections.Immutable.ImmutableList",
			"System.Collections.Immutable.ImmutableQueue",
			"System.Collections.Immutable.ImmutableSortedDictionary",
			"System.Collections.Immutable.ImmutableSortedSet",
			"System.Collections.Immutable.ImmutableStack",
			"System.Collections.Generic.IReadOnlyList",
			"System.Collections.Generic.IReadOnlyDictionary",
			"System.Collections.Generic.IEnumerable",
		}.ToImmutableHashSet();

		/// <summary>
		/// Determine if a given type is mutable.
		/// </summary>
		/// <param name="type">The type to determine mutability for.</param>
		/// <returns>Whether the type is mutable.</returns>
		public bool IsTypeMutable(
			ITypeSymbol type,
			MutabilityInspectionFlags flags = MutabilityInspectionFlags.Default
		) {
			var typesInCurrentCycle = new HashSet<ITypeSymbol>();
			var result = IsTypeMutableRecursive( type, flags, typesInCurrentCycle );
			return result;
		}

		private bool IsTypeMutableRecursive(
			ITypeSymbol type,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			if( type is IErrorTypeSymbol  || type == null ) {
				throw new Exception( $"Type '{type}' cannot be resolved. Please ensure all dependencies " 
					+ "are referenced, including transitive dependencies." );
			}

			if( IsTypeKnownImmutable( type ) ) {
				return false;
			}

			if( type.TypeKind == TypeKind.Array ) {
				return true;
			}

			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute ) && IsTypeMarkedImmutable( type ) ) {
				return false;
			}

			if( typeStack.Contains( type ) ) {
				// We have a cycle. If we're here, it means that either some read-only member causes the cycle (via IsMemberMutableRecursive), 
				// or a generic parameter to a type causes the cycle (via IsTypeMutableRecursive). This is safe if the checks above have 
				// passed and the remaining members are read-only immutable. So we can skip the current check, and allow the type to continue 
				// to be evaluated.
				return false;
			}

			typeStack.Add( type );
			try {

				if( ImmutableContainerTypes.Contains( type.GetFullTypeName() ) ) {
					var namedType = type as INamedTypeSymbol;
					bool isMutable = namedType.TypeArguments.Any( t => IsTypeMutableRecursive( t, MutabilityInspectionFlags.Default, typeStack ) );
					return isMutable;
				}

				if( type.TypeKind == TypeKind.Interface ) {
					return true;
				}

				if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
						&& type.TypeKind == TypeKind.Class
						&& !type.IsSealed
					) {
					return true;
				}

				foreach( ISymbol member in type.GetNonStaticMembers() ) {
					if( IsMemberMutableRecursive( member, MutabilityInspectionFlags.Default, typeStack ) ) {
						return true;
					}
				}

				return false;

			} finally {
				typeStack.Remove( type );
			}
		}

		private bool IsMemberMutableRecursive(
			ISymbol symbol,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {

			switch( symbol.Kind ) {

				case SymbolKind.Property:

					var prop = (IPropertySymbol)symbol;

					var sourceTree = prop.DeclaringSyntaxReferences.FirstOrDefault();
					var declarationSyntax = sourceTree?.GetSyntax() as PropertyDeclarationSyntax;
					if( declarationSyntax != null && declarationSyntax.IsPropertyGetterImplemented() ) {
						// property has getter with body; it is either backed by a field, or is a static function; ignore
						return false;
					}

					if( IsPropertyMutable( prop ) ) {
						return true;
					}

					if( IsTypeMutableRecursive( prop.Type, flags, typeStack ) ) {
						return true;
					}

					return false;

				case SymbolKind.Field:

					var field = (IFieldSymbol)symbol;

					if( IsFieldMutable( field ) ) {
						return true;
					}

					if( IsTypeMutableRecursive( field.Type, flags, typeStack ) ) {
						return true;
					}

					return false;

				case SymbolKind.Method:
				case SymbolKind.NamedType:
					// ignore these symbols, because they do not contribute to immutability
					return false;

				default:
					// we've got a member (event, etc.) that we can't currently be smart about, so fail
					return true;
			}
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
		/// This does not check if the type of the property is also mutable; use <see cref="IsTypeMutable"/> for that.
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
		/// This does not check if the type of the field is also mutable; use <see cref="IsTypeMutable"/> for that.
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
