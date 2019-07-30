using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation {
	partial class DefaultStructCreationAnalyzer {

		[ExportCodeFixProvider(
			LanguageNames.CSharp,
			Name = nameof( UseSuggestedInitializerCodefix )
		)]
		public sealed class UseSuggestedInitializerCodefix : CodeFixProvider {

			public override ImmutableArray<string> FixableDiagnosticIds
				=> ImmutableArray.Create( Diagnostics.DontCallDefaultStructConstructor.Id );

			public override FixAllProvider GetFixAllProvider()
				=> WellKnownFixAllProviders.BatchFixer;

			public override async Task RegisterCodeFixesAsync(
				CodeFixContext ctx
			) {
				var root = await ctx.Document
					.GetSyntaxRootAsync( ctx.CancellationToken )
					.ConfigureAwait( false );

				foreach( var diagnostic in ctx.Diagnostics ) {
					var span = diagnostic.Location.SourceSpan;

					SyntaxNode syntax = root.FindNode( span, getInnermostNodeForTie: true );
					if( !( syntax is ObjectCreationExpressionSyntax creationExpression ) ) {
						continue;
					}

					ctx.RegisterCodeFix(
						CodeAction.Create(
							title: diagnostic.Properties[ACTION_TITLE_KEY],
							createChangedDocument: ct => {
								SyntaxNode replacement = SyntaxFactory.ParseExpression(
									diagnostic.Properties[REPLACEMENT_KEY]
								);

								SyntaxNode newRoot = root.ReplaceNode( creationExpression, replacement );
								Document newDoc = ctx.Document.WithSyntaxRoot( newRoot );

								return Task.FromResult( newDoc );
							}
						),
						diagnostic
					);
				}
			}
		}
	}
}