using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ValueSourceStringsAnalyzer : DiagnosticAnalyzer {
		
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ValueSourceStrings );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( Register );
		}

		private void Register( CompilationStartAnalysisContext compilation ) {
			compilation.RegisterSyntaxNodeAction(
				AnalyzeSyntaxNode,
				SyntaxKind.Parameter
			);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as ParameterSyntax;
			if( root == null ) {
				return;
			}

			var attributes = root.AttributeLists.SelectMany( x => x.Attributes ).ToArray();

			// Parameter has no attributes
			if( !attributes.Any() ) {
				return;
			}

			foreach( var attribute in attributes ) {
				var symbol = context.SemanticModel.GetSymbolInfo( attribute ).Symbol;

				if( symbol == null ) {
					continue;
				}

				// Not a [ValueSource()]
				if( symbol.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat ) != "ValueSourceAttribute" ) {
					continue;
				}

				var arguments = attribute.ArgumentList.Arguments;

				if( arguments.Count != 1 ) {
					continue;
				}

				var argExpression = arguments.First().Expression;

				// Not [ValueSource( "foo" )]
				if( !argExpression.IsKind( SyntaxKind.StringLiteralExpression ) ) {
					continue;
				}

				var diagnostic = Diagnostic.Create( Diagnostics.ValueSourceStrings, argExpression.GetLocation(), ( ( LiteralExpressionSyntax )argExpression ).ToString().Trim( '"' ) );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}
