using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal sealed class AssertIsBoolDiagnostic {

		public string Message { get; }
		public ExpressionSyntax Replacement { get; }

		public AssertIsBoolDiagnostic( string message, ExpressionSyntax replacement ) {
			Message = message;
			Replacement = replacement;
		}
	}
}
