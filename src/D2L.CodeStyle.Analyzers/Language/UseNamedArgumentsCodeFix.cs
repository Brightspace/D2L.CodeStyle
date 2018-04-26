using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( UseNamedArgumentsCodeFix )
	)]
	public sealed class UseNamedArgumentsCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.UseNamedArgsForInvocationWithLotsOfArgs.Id
			);

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync( CodeFixContext ctx ) {
			var root = await ctx.Document
				.GetSyntaxRootAsync( ctx.CancellationToken )
				.ConfigureAwait( false );

			foreach( var diagnostic in ctx.Diagnostics ) {
				var span = diagnostic.Location.SourceSpan;

				var invocation = (InvocationExpressionSyntax)root.FindNode( span );
				var args = invocation.ArgumentList;

				// The names to add to arguments was stored in the diagnostic
				var argNames = diagnostic.Properties
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
								argNames,
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
			ImmutableDictionary<int, string> argNames,
			CancellationToken cancellationToken
		) {
			var namedArgs = GetNamedArgs( args, argNames );

			var newArgs = args
				.WithArguments(
					SyntaxFactory.SeparatedList( namedArgs )
				);

			var newRoot = root.ReplaceNode( args, newArgs );
			var newDoc = orig.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}

		private static IEnumerable<ArgumentSyntax> GetNamedArgs(
			ArgumentListSyntax args,
			ImmutableDictionary<int, string> argNames
		) {
			for( var idx = 0; idx < args.Arguments.Count; idx ++ ) {
				var arg = args.Arguments[idx];

				// Some args might already be named
				if ( !argNames.ContainsKey( idx ) ) {
					yield return arg;
					continue;
				}

				// TODO: get whitespace to at least preserve. More D2Ly would
				// be to split onto multi lines but that has some edge-cases...

				yield return arg
					.WithNameColon(
						SyntaxFactory.NameColon( argNames[idx] )
					);
			}
		}
	}
}
