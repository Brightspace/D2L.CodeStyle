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

		private const string Title = "Ensure test does not contain named property 'Throws' in TestCaseData.";
		private const string Description = "Named property 'Throws' should not be used in TestCaseData.";
		internal const string MessageFormat = "Use Assert.Throws or Assert.That in your test case instead of 'Throws' in TestCaseData for NUnit 3 compatibility";

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

			if( root.Expression is MemberAccessExpressionSyntax ) {
				var memberAccessExpression = root.Expression as MemberAccessExpressionSyntax;
				if( memberAccessExpression.Name.ToString().Equals( "Throws" ) ) {
					var objectCreationExpressions = memberAccessExpression.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().ToImmutableArray();
					if( objectCreationExpressions.Length > 0 ) {
						if( objectCreationExpressions[0].Type.ToString().Equals( "TestCaseData" ) ) {
							var diagnostic = Diagnostic.Create( Rule, memberAccessExpression.Name.GetLocation() );
							context.ReportDiagnostic( diagnostic );
						}
					} else {
						var identifierNames = memberAccessExpression.DescendantNodes().OfType<IdentifierNameSyntax>().ToImmutableArray();
						foreach( var identifierName in identifierNames ) {
							var symbol = context.SemanticModel.GetSymbolInfo( identifierName ).Symbol;
							if( symbol != null && symbol.DeclaringSyntaxReferences != null ) {
								var node = symbol.DeclaringSyntaxReferences[0].GetSyntax();
								if( node is VariableDeclaratorSyntax ) {
									var variableDeclaration = node as VariableDeclaratorSyntax;
									if( variableDeclaration.Initializer.Value.ToString().Contains( "TestCaseData" ) ) {
										var diagnostic = Diagnostic.Create( Rule, memberAccessExpression.Name.GetLocation() );
										context.ReportDiagnostic( diagnostic );
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
