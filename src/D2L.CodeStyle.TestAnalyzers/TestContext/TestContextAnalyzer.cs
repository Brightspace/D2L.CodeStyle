using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.TestContext {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class TestContextAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0011";
		private const string Category = "Safety";

		private const string Title = "Ensure test does not contain 'TestContext.CurrentContext.Result.Status' or 'TestContext.CurrentContext.Result.State'.";
		private const string Description = "'TestContext.CurrentContext.Result.Status' and 'TestContext.CurrentContext.Result.State' should not be used in tests.";
		internal const string MessageFormat = "Do not use 'TestContext.CurrentContext.Result.Status' and 'TestContext.CurrentContext.Result.State' for NUnit 3 compatibility";

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
				SyntaxKind.SimpleMemberAccessExpression
			);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as MemberAccessExpressionSyntax;
			if( root == null ) {
				return;
			}

			if( root.ToString().Equals( "TestContext.CurrentContext.Result.Status" ) || root.ToString().Equals( "TestContext.CurrentContext.Result.State" ) ) {
				var diagnostic = Diagnostic.Create( Rule, root.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}
