using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Threading {

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseConfigureAwaitFalseAnalyzerFixer)), Shared]
	public class UseConfigureAwaitFalseAnalyzerFixer : CodeFixProvider {
		private static readonly string s_title = "Awaitable's should specify a ConfigureAwait";
		public override async Task RegisterCodeFixesAsync(CodeFixContext context) {

			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			var awaitExpression =
				root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().First();

			context.RegisterCodeFix(
				CodeAction.Create(s_title, c => AddConfigureAwait(context.Document, awaitExpression, c), s_title),
				diagnostic);

		}

		private async Task<Document> AddConfigureAwait(
			Document document,
			AwaitExpressionSyntax awaitExpression,
			CancellationToken ct
		) {

			var configureAwaitExpr = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				awaitExpression.Expression,
				SyntaxFactory.IdentifierName("ConfigureAwait"));

			var invocExpression =
				SyntaxFactory.InvocationExpression(configureAwaitExpr)
							 .WithArgumentList(
								 SyntaxFactory.ArgumentList(
									 SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
										 SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))));

			var newAwait = SyntaxFactory.AwaitExpression(invocExpression);

			var root = await document.GetSyntaxRootAsync(ct);
			var newRoot = root.ReplaceNode(awaitExpression, newAwait);

			var newDoc = document.WithSyntaxRoot(newRoot);

			return newDoc;


		}

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create<string>(UseConfigureAwaitFalseAnalyzer.DIAGNOSTIC_ID); }
		}

	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UseConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer {

		public const string DIAGNOSTIC_ID = "D2LAwaitFalse";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
		private static readonly LocalizableString s_title = "Awaitable's should specify a ConfigureAwait";
		private static readonly LocalizableString s_description = "Awaitable's should use ConfigureAwait(false)";
		private const string CATEGORY = "Naming";

		private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(DIAGNOSTIC_ID, s_title, s_description, CATEGORY, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: s_description);


		public override void Initialize(AnalysisContext context) {
			context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.AwaitExpression);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get { return ImmutableArray.Create<DiagnosticDescriptor>(s_rule); }
		}

		private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context) {

			bool isConfigured = context.Node.DescendantNodes()
					.OfType<MemberAccessExpressionSyntax>()
					.Any(x => IsConfigureAwaitFunction(x, context.SemanticModel));

			if (!isConfigured) {

				var diagnostic = Diagnostic.Create(s_rule, context.Node.GetLocation());

				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool IsConfigureAwaitFunction(MemberAccessExpressionSyntax node, SemanticModel model) {
			if (!node.IsKind(SyntaxKind.SimpleMemberAccessExpression)) {
				return false;
			}

			var symbol = model.GetSymbolInfo(node).Symbol;

			if (symbol == null) {
				return false;
			}

			if (!String.Equals(symbol.ContainingAssembly.Identity.Name, "mscorlib", StringComparison.OrdinalIgnoreCase)) {
				return false;
			}

			if (!String.Equals(symbol.ContainingNamespace.ToString(), "System.Threading.Tasks", StringComparison.OrdinalIgnoreCase)) {
				return false;
			}

			if (symbol.Name.Equals("ConfigureAwait", StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			return false;
		}
	}
}
