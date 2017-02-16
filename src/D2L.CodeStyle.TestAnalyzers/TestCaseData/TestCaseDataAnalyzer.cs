using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.TestCaseData {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class TestCaseDataAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0010";
		private const string Category = "Safety";

		private const string Title = "Ensure test does not contain named property 'Throws' and 'MakeExplicit' in TestCaseData.";
		private const string Description = "Named property 'Throws' and 'MakeExplicit' should not be used in TestCaseData.";
		internal const string MessageFormat = "{0} in TestCaseData for NUnit 3 compatibility";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

		public override void Initialize( AnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				AnalyzeSyntaxNode,
				SyntaxKind.InvocationExpression
			);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as InvocationExpressionSyntax;
			if( root == null ) {
				return;
			}

			var symbol = context.SemanticModel.GetSymbolInfo( root );
			if( symbol.Symbol == null ) {
				return;
			}

			var method = symbol.Symbol as IMethodSymbol;
			if( method == null ) {
				return;
			}

			if( method.Name == "Throws" && method.ContainingType.Name == "TestCaseData" ) {
				var diagnostic = Diagnostic.Create( Rule, root.GetLocation(), "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
				context.ReportDiagnostic( diagnostic );
			} else if( method.Name == "MakeExplicit" && method.ContainingType.Name == "TestCaseData" ) {
				var diagnostic = Diagnostic.Create( Rule, root.GetLocation(), "Do not use 'MakeExplicit'" );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}
