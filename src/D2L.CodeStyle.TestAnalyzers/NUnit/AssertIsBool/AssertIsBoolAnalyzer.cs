using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed partial class AssertIsBoolAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
			ImmutableArray.Create( Diagnostics.MisusedAssertIsTrueOrFalse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(
				AnalyzeInvocation,
				SyntaxKind.InvocationExpression
			);
		}

		private static void AnalyzeInvocation( 
			SyntaxNodeAnalysisContext ctx 
		) {
			var invocation = ctx.Node as InvocationExpressionSyntax;
			if( invocation == null ) {
				return;
			}

			ISymbol symbol = ctx.SemanticModel
				.GetSymbolInfo( invocation.Expression )
				.Symbol;

			if( symbol == null ) {
				return;
			}

			string symbolName;
			if( !AssertIsBoolSymbols.TryGetName( symbol, out symbolName ) ) {
				return;
			}

			if( AssertIsBoolBinaryExpressions.TryGetDiagnosticProvider(
				invocation,
				out AssertIsBoolDiagnosticProvider diagnosticProvider ) 
			) {
				AssertIsBoolDiagnostic diagnostic = diagnosticProvider.GetDiagnostic( symbolName );
				ReportDiagnostic( ctx, symbolName, diagnostic );
			}
		}

		private static void ReportDiagnostic(
			SyntaxNodeAnalysisContext ctx,
			string symbolName,
			AssertIsBoolDiagnostic diagnostic
		) {
			ImmutableDictionary<string, string> properties =
				new Dictionary<string, string> { { "replacement", diagnostic.Replacement.ToFullString() } }.ToImmutableDictionary();
			Diagnostic analysisDiagnostic = Diagnostic.Create(
					Diagnostics.MisusedAssertIsTrueOrFalse,
					ctx.Node.GetLocation(),
					properties,
					symbolName,
					diagnostic.Message 
				);
			ctx.ReportDiagnostic( analysisDiagnostic );
		}
	}
}
