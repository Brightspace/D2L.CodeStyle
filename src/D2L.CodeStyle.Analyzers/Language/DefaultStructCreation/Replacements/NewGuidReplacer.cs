using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.Replacements {

	internal sealed class NewGuidReplacer : IDefaultStructCreationReplacer {
		string IDefaultStructCreationReplacer.Title { get; } = "Use Guid.NewGuid()";

		private readonly INamedTypeSymbol m_guidType;

		public NewGuidReplacer(
			Compilation compilation
		) {
			m_guidType = compilation.GetTypeByMetadataName( "System.Guid" );
		}

		bool IDefaultStructCreationReplacer.CanReplace(
			INamedTypeSymbol structType
		) {
			if( m_guidType == null ) {
				return false;
			}

			if( m_guidType.TypeKind == TypeKind.Error ) {
				return false;
			}

			if( m_guidType != structType ) {
				return false;
			}

			return true;
		}

		SyntaxNode IDefaultStructCreationReplacer.GetReplacement(
			INamedTypeSymbol structType,
			TypeSyntax structName
		) {
			return SyntaxFactory
				.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						structName,
						SyntaxFactory.IdentifierName( "NewGuid" )
					)
				);
		}

	}
}