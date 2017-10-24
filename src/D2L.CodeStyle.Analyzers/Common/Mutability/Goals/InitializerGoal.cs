using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct InitializerGoal : Goal {
		public InitializerGoal(
			ITypeSymbol type,
			ExpressionSyntax expr
		) {
			Type = type;
			Expr = expr;
		}

		public ITypeSymbol Type { get; }
		public ExpressionSyntax Expr { get; }
	}
}
