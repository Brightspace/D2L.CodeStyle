using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.Asserts {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class AssertsAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0006";
		private const string Category = "Safety";

		private const string Title = "Ensure test does not contain Assert.IsNullOrEmpty and Assert.IsNotNullOrEmpty.";
		private const string Description = "'Assert.IsNullOrEmpty' and 'Assert.IsNotNullOrEmpty' should not be used in tests.";
		internal const string MessageFormat = "Use Assert.That rather than 'Assert.IsNullOrEmpty' or 'Assert.IsNotNullOrEmpty' for NUnit 3 compatibility";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
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

			if( root.Expression.ToString().Equals( "Assert.IsNullOrEmpty" ) || root.Expression.ToString().Equals( "Assert.IsNotNullOrEmpty" ) ) {
				var diagnostic = Diagnostic.Create( Rule, root.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
			
		}
	}
}
