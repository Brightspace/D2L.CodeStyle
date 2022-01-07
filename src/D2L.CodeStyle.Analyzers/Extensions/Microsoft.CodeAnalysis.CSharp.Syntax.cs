using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {
		public static bool IsAutoImplemented( this PropertyDeclarationSyntax syntax ) {
			// Remember: auto properties are the ones that hold state. Other
			// properties are just sugar around function calls.

			// For expression bodied properties like
			//   public MyType MyProp => expr;
			// expr is invoked for every access so this behaves the same as
			//   public MyType MyProp { get { return expr; } }

			// Conveniently enough expr is stored in the initializer field of
			// the PropertyDeclarationSyntax, so the code for analyzing fields
			// will narrow the inspected type to that of expr.
			if ( syntax.AccessorList == null ) {
				return false;
			}

			// Auto-implemented properties have at least an implicit (no body)
			// get and never have an explicit set.
			foreach( var accessor in syntax.AccessorList.Accessors ) {
				if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration
			 	 || accessor.Kind() == SyntaxKind.SetAccessorDeclaration
				) {
					if ( accessor.Body != null ) {
						return false;
					}
				}
			}

			return true;
		}

		public static bool IsAttributeOfType(
				this SemanticModel model,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol attributeType
			) {

			TypeInfo typeInfo = model.GetTypeInfo( attributeSyntax );

			ITypeSymbol typeSymbol = typeInfo.Type;
			if( typeSymbol == null ) {
				return false;
			}

			if( !typeSymbol.Equals( attributeType, SymbolEqualityComparer.Default ) ) {
				return false;
			}

			return true;
		}

		public static bool IsFromDocComment( this SyntaxNode node )
			=> node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>() != null;

		public static bool IsStaticFunction( this ExpressionSyntax expr ) {
			if ( expr is not AnonymousFunctionExpressionSyntax afes ) {
				return false;
			}

			return afes.Modifiers.Any(
				m => m.IsKind( SyntaxKind.StaticKeyword )
			);
		}

		public static bool IsPublic( this BaseMethodDeclarationSyntax method ) {
			return method.Modifiers.IndexOf( SyntaxKind.PublicKeyword ) >= 0;
		}

		public static bool IsStatic( this BaseMethodDeclarationSyntax method ) {
			return method.Modifiers.IndexOf( SyntaxKind.StaticKeyword ) >= 0;
		}
	}
}
