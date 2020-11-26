using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {
		#region Stuff that can be deleted with  ImmutableGenericArgumentAnalyzer
		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = ImmutableHashSet.Create(
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.UTF8Encoding",
			"System.IO.Abstractions.IFileSystem"
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

		private static bool IsExternallyOwnedMarkedImmutableType( this ITypeSymbol symbol ) {
			return MarkedImmutableTypes.Contains( symbol.GetFullTypeName() );
		}

		#endregion

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

		/// <summary>
		/// Find the matching type argument in the base interface
		/// that corresponds to this type parameter.  That is,
		/// if we have Foo<S, T>: IFoo<S>, IBar<T>, this will
		/// match up the Foo S, to the IFoo S, but will get -1
		/// from IBar since it doesn't have S.
		/// </summary>
		public static int IndexOfArgument(
			this INamedTypeSymbol intf,
			string name
		) {

			for( int ordinal = 0; ordinal < intf.TypeArguments.Length; ordinal++ ) {
				if( string.Equals( intf.TypeArguments[ ordinal ].Name, name, StringComparison.Ordinal ) ) {
					return ordinal;
				}
			}

			return -1;
		}

		public static bool TryGetTypeByMetadataName(
				this Compilation compilation,
				string fullyQualifiedMetadataName,
				out INamedTypeSymbol typeSymbol
			) {

			typeSymbol = compilation.GetTypeByMetadataName( fullyQualifiedMetadataName );
			return ( typeSymbol != null );
		}

		// Inspired by https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/InternalUtilities/GeneratedCodeUtilities.cs
		public static bool IsFromGeneratedCode( this ISymbol symbol ) {
			var root = symbol.DeclaringSyntaxReferences.Single()
				.SyntaxTree.GetRoot();

			return IsGeneratedCodeFile( root ) || BeginsWithAutoGeneratedComment( root );
		}

		private static bool IsGeneratedCodeFile( SyntaxNode root ) {
			var path = root.SyntaxTree.FilePath.ToLower();

			if ( string.IsNullOrEmpty( path ) ) {
				return false;
			}

			// foo/bar/baz.a.b.c --> baz.a.b
			var end = Path.GetFileNameWithoutExtension( path );

			if ( end.EndsWith( ".designer" ) ) {
				return true;
			}

			if ( end.EndsWith( ".generated" ) ) {
				return true;
			}

			if ( end.EndsWith( ".g" ) ) {
				return true;
			}

			if ( end.EndsWith( ".g.i" ) ) {
				return true;
			}

			return false;
		}

		private static bool BeginsWithAutoGeneratedComment( SyntaxNode root ) {
			if ( !root.HasLeadingTrivia ) {
				return false;
			}

			foreach( var trivia in  root.GetLeadingTrivia() ) {
				bool isComment = trivia.IsKind( SyntaxKind.SingleLineCommentTrivia )
					|| trivia.IsKind( SyntaxKind.MultiLineCommentTrivia );

				if ( !isComment ) {
					continue;
				}

				var text = trivia.ToString();

				if( text.Contains( "<autogenerated" ) ) {
					return true;
				}

				if ( text.Contains( "<auto-generated" ) ) {
					return true;
				}
			}

			return false;
		}
	}
}
