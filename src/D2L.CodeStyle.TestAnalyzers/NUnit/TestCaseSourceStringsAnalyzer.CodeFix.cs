using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {
	partial class TestCaseSourceStringsAnalyzer {
		[ExportCodeFixProvider( LanguageNames.CSharp )]
		public sealed class CodeFix : CodeFixProvider {

			private const string TITLE = "nameofify";

			public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create( Diagnostics.TestCaseSourceStrings.Id );

			public override FixAllProvider GetFixAllProvider() {
				return WellKnownFixAllProviders.BatchFixer;
			}

			public sealed override async Task RegisterCodeFixesAsync( CodeFixContext context ) {
				var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );

				foreach( var diagnostic in context.Diagnostics ) {
					var span = diagnostic.Location.SourceSpan;

					var stringArg = root.FindToken( span.Start ).Parent;

					context.RegisterCodeFix( CodeAction.Create(
						title: TITLE,
						createChangedDocument: cancellationToken => NameofIfyAsync( context.Document, stringArg, cancellationToken )
					), diagnostic );
				}
			}

			private async Task<Document> NameofIfyAsync( Document document, SyntaxNode arg, CancellationToken cancellationToken ) {
				// Get the name of the identifier from the string literal
				var identifier = arg.ToFullString().Trim( ' ', '"' );

				var newNode = SyntaxFactory.InvocationExpression( SyntaxFactory.IdentifierName( "nameof" ) )
					.WithArgumentList(
						SyntaxFactory
							.ArgumentList(
								SyntaxFactory.SingletonSeparatedList( SyntaxFactory.Argument( SyntaxFactory.IdentifierName( SyntaxFactory.Identifier(
									SyntaxFactory.TriviaList(),
									identifier,
									// Add a trailing space after the argument
									SyntaxFactory.TriviaList( SyntaxFactory.Space )
								) ) ) ) )
							// Add a leading space before the argument
							.WithOpenParenToken( SyntaxFactory.Token( SyntaxFactory.TriviaList(), SyntaxKind.OpenParenToken, SyntaxFactory.TriviaList( SyntaxFactory.Space ) ) )
					);

				var oldRoot = await document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
				var newRoot = oldRoot.ReplaceNode( arg, newNode );

				return document.WithSyntaxRoot( newRoot );
			}
		}
	}
}
