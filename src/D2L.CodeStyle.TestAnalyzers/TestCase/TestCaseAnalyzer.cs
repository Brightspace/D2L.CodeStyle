using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.TestCase {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class TestCaseAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0005";
		private const string Category = "Safety";

		private const string Title = "Ensure test does not contain named parameters 'Result' in TestCaseAttribute.";
		private const string Description = "Named parameters 'Result' should not be used in TestCaseAttribute.";
		internal const string MessageFormat = "Use 'ExpectedResult' rather than 'Result' for NUnit 3 compatibility";

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
				SyntaxKind.AttributeList
			);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as AttributeListSyntax;
			if( root == null ) {
				return;
			}
			
			foreach( var attribute in root.Attributes ) {
				if( attribute.Name.ToString().Equals( "TestCase" ) ) {
					var attributeArguments = attribute.ArgumentList.Arguments.ToImmutableArray();
					foreach( var attributeArgument in attributeArguments ) {
						if( attributeArgument.NameEquals != null && attributeArgument.NameEquals.Name.ToString().Equals( "Result" ) ) {
							var diagnostic = Diagnostic.Create( Rule, attributeArgument.GetLocation() );
							context.ReportDiagnostic( diagnostic );
						}
					}
				}
			}
		}
	}
}
