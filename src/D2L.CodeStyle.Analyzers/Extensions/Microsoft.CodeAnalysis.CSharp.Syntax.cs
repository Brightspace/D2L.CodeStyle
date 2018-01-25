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
	}
}
