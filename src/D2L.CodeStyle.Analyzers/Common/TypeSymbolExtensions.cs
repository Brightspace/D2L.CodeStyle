using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.Common {

	public static class SyntaxNodeExtension {
		public static bool IsPropertyGetterImplemented( this PropertyDeclarationSyntax syntax ) {
			var getter = syntax.AccessorList?.Accessors.FirstOrDefault( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) );
			return getter?.Body != null;
		}
	}

	public static class TypeSymbolExtensions {

		private readonly static ImmutableArray<SpecialType> PrimitiveTypes = ImmutableArray.Create(
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
			SpecialType.System_UIntPtr
		);

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

		public static IEnumerable<ISymbol> GetExplicitNonStaticMembers( this INamespaceOrTypeSymbol type ) {

			return type.GetMembers()
				.Where( t => !t.IsStatic && !t.IsImplicitlyDeclared );
		}

		public static bool IsPrimitive( this ITypeSymbol symbol ) {
			return PrimitiveTypes.Contains( symbol.SpecialType );
		}
	}
}