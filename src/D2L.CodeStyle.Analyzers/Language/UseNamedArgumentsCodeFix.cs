using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Language {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( UseNamedArgumentsCodeFix )
	)]
	public sealed class UseNamedArgumentsCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.TooManyUnnamedArgs.Id
			);

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}


		public override async Task RegisterCodeFixesAsync(
			CodeFixContext ctx
		) {
			var root = await ctx.Document
				.GetSyntaxRootAsync( ctx.CancellationToken )
				.ConfigureAwait( false );

			foreach( var diagnostic in ctx.Diagnostics ) {
				var span = diagnostic.Location.SourceSpan;

				var args = GetArgs( root, ctx.Span );

				// The analyzer stored the names to add to arguments in the
				// diagnostic.
				var paramNames = diagnostic.Properties
					.ToImmutableDictionary(
						kvp => int.Parse( kvp.Key ),
						kvp => kvp.Value
					);

				ctx.RegisterCodeFix(
					CodeAction.Create(
						title: "Use named arguments",
						createChangedDocument: ct =>
							UseNamedArgs(
								ctx.Document,
								root,
								args,
								paramNames,
								ct
							)
					),
					diagnostic
				);
			}
		}

		private static Task<Document> UseNamedArgs(
			Document orig,
			SyntaxNode root,
			ArgumentListSyntax args,
			ImmutableDictionary<int, string> paramNames,
			CancellationToken cancellationToken
		) {

			var namedArgs = GetNamedArgs( args, paramNames );

			var newArgs = SyntaxFactory.ArgumentList(
				openParenToken: args.OpenParenToken,
				arguments: SyntaxFactory.SeparatedList(
					nodes: namedArgs,
					separators: args.Arguments.GetSeparators()
				),
				closeParenToken: args.CloseParenToken
			);


			var newRoot = root.ReplaceNode( args, newArgs );
			var newDoc = orig.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}

		private static IEnumerable<ArgumentSyntax> GetNamedArgs(
			ArgumentListSyntax args,
			ImmutableDictionary<int, string> paramNames
		) {
			for( var idx = 0; idx < args.Arguments.Count; idx ++ ) {
				var arg = args.Arguments[idx];

				// Some args might already be named
				if ( !paramNames.ContainsKey( idx ) ) {
					yield return arg;
					continue;
				}

				SyntaxTriviaList leadingTrivia =
					arg.RefOrOutKeyword.Kind() == SyntaxKind.None
					? arg.Expression.GetLeadingTrivia()
					: arg.RefOrOutKeyword.LeadingTrivia;

				yield return arg
					.WithNameColon(
						SyntaxFactory.NameColon( paramNames[idx] )
							.WithLeadingTrivia( leadingTrivia )
					);
			}
		}

		public static ArgumentListSyntax GetArgs(
			SyntaxNode root,
			TextSpan span
		) {
			var node = root
				.FindNode(
					span,
					getInnermostNodeForTie: true
				);

			var args = RequireNamedArgumentsAnalyzer.GetArgs( node );

			return args;
		}
	}
}
