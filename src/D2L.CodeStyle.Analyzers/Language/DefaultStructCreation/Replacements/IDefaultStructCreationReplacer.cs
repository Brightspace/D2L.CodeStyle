using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.Replacements {

	internal interface IDefaultStructCreationReplacer {

		string Title { get; }

		bool CanReplace(
			INamedTypeSymbol structType
		);

		SyntaxNode GetReplacement(
			INamedTypeSymbol structType,
			TypeSyntax structName
		);

	}
}