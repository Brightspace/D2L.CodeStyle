using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	partial class AssertIsBoolAnalyzer {

		[ExportCodeFixProvider( LanguageNames.CSharp )]
		public sealed class CodeFix : CodeFixProvider {

			public override ImmutableArray<string> FixableDiagnosticIds => 
				ImmutableArray.Create( Diagnostics.MisusedAssertIsTrueOrFalse.Id );

			public override FixAllProvider GetFixAllProvider() {
				return WellKnownFixAllProviders.BatchFixer;
			}

			public override async Task RegisterCodeFixesAsync( CodeFixContext context ) {
				SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );

				foreach( Diagnostic diagnostic in context.Diagnostics ) {
					TextSpan span = diagnostic.Location.SourceSpan;

					SyntaxNode assertNode = root.FindNode( span );
					string replacement = diagnostic.Properties[ "replacement" ];

					context.RegisterCodeFix( CodeAction.Create(
						title: Diagnostics.MisusedAssertIsTrueOrFalse.Title.ToString(),
						createChangedDocument: cancellationToken => ReplaceNode( context.Document, assertNode, replacement, cancellationToken )
					), diagnostic );
				}
			}

			private async Task<Document> ReplaceNode( Document document, SyntaxNode node, string replacement, CancellationToken cancellationToken ) {
				// Get the name of the identifier from the string literal

				ExpressionSyntax newNode = SyntaxFactory.ParseExpression( replacement );

				SyntaxNode oldRoot = await document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
				SyntaxNode newRoot = oldRoot.ReplaceNode( node, newNode );

				return document.WithSyntaxRoot( newRoot );
			}

		}

	}
}
