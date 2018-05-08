using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class AssertIsBoolAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.MisusedAssertIsTrueOrFalse );

		public const string AssertIsTrue = "NUnit.Framework.Assert.IsTrue";
		public const string AssertIsFalse = "NUnit.Framework.Assert.IsFalse";

		private static readonly ImmutableHashSet<string> AnalyzedSymbols =
			new[] { AssertIsTrue, AssertIsFalse }.ToImmutableHashSet();

		private static readonly SymbolDisplayFormat MethodDisplayFormat = new SymbolDisplayFormat(
			memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
			localOptions: SymbolDisplayLocalOptions.IncludeType,
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
		);

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

			string symbolName = symbol.ToDisplayString( MethodDisplayFormat );
			if( !AnalyzedSymbols.Contains( symbolName ) ) {
				return;
			}

			ArgumentSyntax firstArgument = invocation.ArgumentList.Arguments[ 0 ];
			if( firstArgument.Expression is BinaryExpressionSyntax binaryExpression ) {

				AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> diagnosticProvider;
				if( !AssertIsBoolBinaryExpressions.TryGetDiagnosticProvider( binaryExpression, out diagnosticProvider ) ) {
					// if we don't know it; we leave it
					return;
				}

				string diagnosticMessage = diagnosticProvider.GetDiagnosticFunc( symbolName )( binaryExpression );
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
