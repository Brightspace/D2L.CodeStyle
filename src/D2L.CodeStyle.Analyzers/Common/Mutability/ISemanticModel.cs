using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability {
	/// <summary>
	/// A mockable/simplified SemanticModel
	/// </summary>
	internal interface ISemanticModel {
		IAssemblySymbol Assembly();
		ITypeSymbol GetTypeForSyntax( SyntaxNode node );
	}
}
