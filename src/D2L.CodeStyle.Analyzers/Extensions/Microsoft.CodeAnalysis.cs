using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Immutability;
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

		public static ImmutabilityScope GetImmutabilityScope( this ITypeSymbol type ) {
			if( type.IsTypeMarkedImmutable() ) {
				return ImmutabilityScope.SelfAndChildren;
			}

			if( type.IsTypeMarkedImmutableBaseClass() ) {
				return ImmutabilityScope.Self;
			}

			return ImmutabilityScope.None;
		}

		private static bool IsTypeMarkedImmutable( this ITypeSymbol symbol ) {
			if( symbol.IsExternallyOwnedMarkedImmutableType() ) {
				return true;
			}
			if( symbol.IsMarkedImmutableGeneric() ) {
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

		private static bool IsTypeMarkedImmutableBaseClass( this ITypeSymbol symbol ) {
			if( Attributes.Objects.ImmutableBaseClass.IsDefined( symbol ) ) {
				return true;
			}
			return false;
		}

		public static bool IsExternallyOwnedMarkedImmutableType( this ITypeSymbol symbol ) {
			return MarkedImmutableTypes.Contains( symbol.GetFullTypeName() );
		}

		private static bool IsMarkedImmutableGeneric( this ITypeSymbol symbol ) {
			var type = symbol as INamedTypeSymbol;
			if( type == null ) {
				return false;
			}

			if( !type.IsGenericType ) {
				return false;
			}

			/*  We can have an annotation in:
			 *  (1) symbol's assembly,
			 *  (2) any of symbol's type arguments's assemblies
			 */
			AttributeData ignore;
			if( type.ContainingAssembly.TryGetImmutableGenericAnnotation( type, out ignore ) ) {
				return true;
			}
			foreach( var typeArgument in type.TypeArguments ) {
				if( typeArgument.ContainingAssembly.TryGetImmutableGenericAnnotation( type, out ignore ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool TryGetImmutableGenericAnnotation( this IAssemblySymbol assembly, ITypeSymbol type, out AttributeData attribute ) {
			attribute = null;

			var attributes = Attributes.Objects.ImmutableGeneric.GetAll( assembly );
			foreach( var attr in attributes ) {

				if( attr.ConstructorArguments.Length != 1 ) {
					continue;
				}

				var arg = attr.ConstructorArguments[0];
				if( arg.Value.Equals( type ) ) {
					attribute = attr;
					return true;
				}
			}

			return false;
		}

		internal static IEnumerable<AttributeData> GetAllImmutableAttributesApplied( this ITypeSymbol type ) {
			var immutable = Attributes.Objects.Immutable.GetAll( type ).FirstOrDefault();
			if( immutable != null ) {
				yield return immutable;
			}

			var immutableBaseClass = Attributes.Objects.ImmutableBaseClass.GetAll( type ).FirstOrDefault();
			if( immutableBaseClass != null ) {
				yield return immutableBaseClass;
			}

			AttributeData immutableGeneric;
			if( type.ContainingAssembly.TryGetImmutableGenericAnnotation( type, out immutableGeneric ) ) {
				yield return immutableGeneric;
			}
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
