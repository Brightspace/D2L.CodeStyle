using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Goals {
	internal struct InitializerGoal : Goal {
		public InitializerGoal( ExpressionSyntax expr ) {
			Expr = expr;
		}

		public ExpressionSyntax Expr { get; }
	}
}
