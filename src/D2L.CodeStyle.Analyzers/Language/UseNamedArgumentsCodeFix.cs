#nullable disable

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
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Language {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( UseNamedArgumentsCodeFix )
	)]
	public sealed class UseNamedArgumentsCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.TooManyUnnamedArgs.Id,
				Diagnostics.LiteralArgShouldBeNamed.Id,
				Diagnostics.NamedArgumentsRequired.Id
			);

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}


		public override async Task RegisterCodeFixesAsync(
			CodeFixContext context
		) {
			var root = await context.Document
				.GetSyntaxRootAsync( context.CancellationToken )
				.ConfigureAwait( false );

			foreach( var diagnostic in context.Diagnostics ) {
				var args = GetArgs( root, context.Span );

				// The analyzer stored the names to add to arguments in the
				// diagnostic.
				var paramNames = diagnostic.Properties
					.ToImmutableDictionary(
						kvp => int.Parse( kvp.Key ),
						kvp => kvp.Value
					);

				context.RegisterCodeFix(
					CodeAction.Create(
						title: "Use named arguments",
						createChangedDocument: ct =>
							UseNamedArgs(
								context.Document,
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
					arg.RefOrOutKeyword.IsKind( SyntaxKind.None )
					? arg.Expression.GetLeadingTrivia()
					: arg.RefOrOutKeyword.LeadingTrivia;

				yield return arg
					.WithoutLeadingTrivia() // Remove leading trivia before argument
					.WithNameColon(
						SyntaxFactory.NameColon( paramNames[idx] )
							.WithLeadingTrivia( leadingTrivia ) // Re-apply leading trivia before NameColon
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

			var args = GetArgs( node );

			return args;
		}

		// Not an extension method because there may be more cases (e.g. in the
		// future) and if more than this fix + its analyzer used this logic
		// there could be undesirable coupling if we handled more cases.
		private static ArgumentListSyntax GetArgs( SyntaxNode syntax ) {
			switch( syntax ) {
				case ImplicitObjectCreationExpressionSyntax implicitObjectCreation:
					return implicitObjectCreation.ArgumentList;
				case InvocationExpressionSyntax invocation:
					return invocation.ArgumentList;
				case ObjectCreationExpressionSyntax objectCreation:
					return objectCreation.ArgumentList;
				case ConstructorInitializerSyntax constructorInitializer:
					return constructorInitializer.ArgumentList;
				default:
					if( syntax.Parent is ArgumentSyntax ) {
						return (ArgumentListSyntax)syntax.Parent.Parent;
					}
					return null;
			}
		}
	}
}
