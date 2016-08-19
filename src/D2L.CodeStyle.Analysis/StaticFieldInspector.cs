using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analysis {
	internal sealed class StaticFieldInspector {
		/// <summary>
		/// Validate that a field is safe for multi-tenant processes
		/// </summary>
		/// <param name="model">Semantic model</param>
		/// <param name="field">The field to inspect</param>
		/// <param name="concern">A diagnostic message</param>
		/// <returns>true if the field is safe</returns>
		public static bool IsMultiTenantSafe( SemanticModel model, FieldDeclarationSyntax field, out string concern ) {
			if ( !field.Modifiers.Any( SyntaxKind.StaticKeyword) ) {
				concern = null;
				return true;
			}

			var typeInfo = model.GetTypeInfo( field.Declaration.Type );

			if (typeInfo.Type.IsValueType ) {
				concern = null;
				return true;
			}

			concern = "Static reference-type fields are scary";
			return false;
		}
	}
}
