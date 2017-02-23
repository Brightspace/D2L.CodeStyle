using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace D2L.CodeStyle.Analyzers.Threading {

	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( UseConfigureAwaitFalseAnalyzerFixer ) ), Shared]
	public class UseConfigureAwaitFalseAnalyzerFixer : CodeFixProvider {

		private static readonly string s_title = "Awaitable should specify a ConfigureAwait";

		public override ImmutableArray<string> FixableDiagnosticIds {
			get { return ImmutableArray.Create<string>( UseConfigureAwaitFalseAnalyzer.DiagnosticId ); }
		}

		public override async Task RegisterCodeFixesAsync( CodeFixContext context ) {

			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );

			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			var awaitExpression =
				root.FindToken( diagnosticSpan.Start ).Parent.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().First();

			context.RegisterCodeFix(
				CodeAction.Create( s_title, c => AddConfigureAwait( context.Document, awaitExpression, c ), s_title ),
				diagnostic );

		}

		private async Task<Document> AddConfigureAwait(
			Document document,
			AwaitExpressionSyntax awaitExpression,
			CancellationToken ct
			) {

			var configureAwaitExpr = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				awaitExpression.Expression,
				SyntaxFactory.IdentifierName( "ConfigureAwait" ) );

			var invocExpression =
				SyntaxFactory.InvocationExpression( configureAwaitExpr )
				             .WithArgumentList(
					             SyntaxFactory.ArgumentList(
						             SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
							             SyntaxFactory.Argument( SyntaxFactory.LiteralExpression( SyntaxKind.FalseLiteralExpression ) ) ) ) );

			var newAwait = SyntaxFactory.AwaitExpression( invocExpression );

			var root = await document.GetSyntaxRootAsync( ct );
			var newRoot = root.ReplaceNode( awaitExpression, newAwait );

			var newDoc = document.WithSyntaxRoot( newRoot );

			return newDoc;

		}

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

	}

}