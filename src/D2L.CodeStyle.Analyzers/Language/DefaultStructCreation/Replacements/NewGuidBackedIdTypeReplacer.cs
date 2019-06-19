using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.Replacements {

	internal sealed class NewGuidBackedIdTypeReplacer : IDefaultStructCreationReplacer {

		public static readonly IDefaultStructCreationReplacer Instance = new NewGuidBackedIdTypeReplacer();

		private NewGuidBackedIdTypeReplacer() { }

		string IDefaultStructCreationReplacer.Title { get; } = "Use GenerateNew()";

		bool IDefaultStructCreationReplacer.CanReplace(
			INamedTypeSymbol structType
		) {
			ImmutableArray<ISymbol> maybeGenerateNew = structType.GetMembers( "GenerateNew" );
			if( maybeGenerateNew.Length != 1 ) {
				return false;
			}

			if( !( maybeGenerateNew[0] is IMethodSymbol generateNew ) ) {
				return false;
			}

			if( generateNew.Parameters.Length != 0 ) {
				return false;
			}

			// Sure seems like one of our Guid-backed IdTypes
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
						SyntaxFactory.IdentifierName( "GenerateNew" )
					)
				);
		}

	}
}