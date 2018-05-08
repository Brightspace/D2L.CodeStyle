using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class AssertIsBoolAnalyzer : DiagnosticAnalyzer {

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

			string symbolName = AssertIsBoolSymbols.GetSymbolName( symbol );
			if( !AssertIsBoolSymbols.IsMatch( symbolName ) ) {
				return;
			}

			ArgumentSyntax firstArgument = invocation.ArgumentList.Arguments[ 0 ];
			if( firstArgument.Expression is BinaryExpressionSyntax binaryExpression ) {

				AssertIsBoolDiagnosticProvider diagnosticProvider;
				if( !AssertIsBoolBinaryExpressions.TryGetDiagnosticProvider( binaryExpression, out diagnosticProvider ) ) {
					// if we don't know it; we leave it
					return;
				}

				string diagnosticMessage = diagnosticProvider.GetDiagnosticFunc( symbolName )();
				ReportDiagnostic( ctx, symbolName, diagnosticMessage );
			}
		}

		private static void ReportDiagnostic(
			SyntaxNodeAnalysisContext ctx,
			string symbolName,
			string diagnosticMessage
		) {
			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.MisusedAssertIsTrueOrFalse,
					ctx.Node.GetLocation(),
					symbolName,
					diagnosticMessage 
				);
			ctx.ReportDiagnostic( diagnostic );
		}
	}
}
