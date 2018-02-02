using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {
		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = ImmutableHashSet.Create(
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.UTF8Encoding"
		);

		public static bool IsTypeMarkedImmutable( this ITypeSymbol symbol ) {
			if( IsTypeMarkedImmutableImpl( symbol ) ) {
				return true;
			}

			foreach( var iface in symbol.AllInterfaces ) {
				if( iface.IsTypeMarkedImmutableImpl() ) {
					return true;
				}
			}

			// for base types, require Inherited = true
			foreach( var baseType in symbol.GetBaseTypes() ) {
				if( IsTypeMarkedImmutableImpl( baseType, requireInherited: true ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsTypeMarkedImmutableImpl( this ITypeSymbol symbol, bool requireInherited = false ) {
			if( symbol.IsExternallyOwnedMarkedImmutableType() ) {
				return true;
			}
			if( Attributes.Objects.Immutable.IsDefined( symbol ) ) {
				if( requireInherited ) {
					var inherited = Attributes.Objects.Immutable.GetValueForInherited( symbol );
					return inherited;
				}
				return true;
			}
			return false;
		}

		public static bool IsExternallyOwnedMarkedImmutableType( this ITypeSymbol symbol ) {
			return MarkedImmutableTypes.Contains( symbol.GetFullTypeName() );
		}

		public static bool IsTypeMarkedSingleton( this ITypeSymbol symbol ) {
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

		private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		private static readonly SymbolDisplayFormat FullTypeWithGenericsDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		public static string GetFullTypeName( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
			return fullyQualifiedName;
		}


		public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeWithGenericsDisplayFormat );
			return fullyQualifiedName;
		}

		private static IEnumerable<INamedTypeSymbol> GetBaseTypes( this ITypeSymbol type ) {
			while( !type.BaseType.IsNullOrErrorType() ) {
				yield return type.BaseType;
				type = type.BaseType;
			}
		}

		public static IEnumerable<ISymbol> GetExplicitNonStaticMembers( this ITypeSymbol type ) {
			return type.GetMembers()
				.Where( t => !t.IsStatic && !t.IsImplicitlyDeclared );
		}

		public static bool IsNullOrErrorType( this ITypeSymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}
			if( symbol.TypeKind == TypeKind.Error ) {
				return true;
			}

			return false;
		}

		public static bool IsNullOrErrorType( this ISymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}

			return false;
		}
	}
}
