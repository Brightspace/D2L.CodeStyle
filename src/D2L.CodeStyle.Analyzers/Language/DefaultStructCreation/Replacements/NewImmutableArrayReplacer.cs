using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.Replacements {

	internal sealed class NewImmutableArrayReplacer : IDefaultStructCreationReplacer {
		string IDefaultStructCreationReplacer.Title { get; } = "Use ImmutableArray<>.Empty";

		private readonly INamedTypeSymbol m_immutableArrayType;

		public NewImmutableArrayReplacer(
			Compilation compilation
		) {
			m_immutableArrayType = compilation
				.GetTypeByMetadataName( "System.Collections.Immutable.ImmutableArray`1" );
		}

		bool IDefaultStructCreationReplacer.CanReplace(
			INamedTypeSymbol structType
		) {
			if( m_immutableArrayType == null ) {
				return false;
			}

			if( m_immutableArrayType.TypeKind == TypeKind.Error ) {
				return false;
			}

			if( m_immutableArrayType != structType.OriginalDefinition ) {
				return false;
			}

			return true;
		}

		SyntaxNode IDefaultStructCreationReplacer.GetReplacement(
			INamedTypeSymbol structType,
			TypeSyntax structName
		) {
			return SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				structName,
				SyntaxFactory.IdentifierName( "Empty" )
			);
		}
	}
}