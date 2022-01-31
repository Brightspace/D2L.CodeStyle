#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Language {
	partial class AwaitedTasksAnalyzer {

		[ExportCodeFixProvider(
			LanguageNames.CSharp,
			Name = nameof( ConfigureAwaitedTaskCodeFix )
		)]
		public sealed class ConfigureAwaitedTaskCodeFix : CodeFixProvider {

			public override ImmutableArray<string> FixableDiagnosticIds
				=> ImmutableArray.Create( Diagnostics.AwaitedTaskNotConfigured.Id );

			public override FixAllProvider GetFixAllProvider()
				=> WellKnownFixAllProviders.BatchFixer;

			public override async Task RegisterCodeFixesAsync(
				CodeFixContext ctx
			) {
				var root = await ctx.Document
					.GetSyntaxRootAsync( ctx.CancellationToken )
					.ConfigureAwait( false );

				bool useSafeAsync = await CanReferenceSafeAsync( ctx )
					.ConfigureAwait( false );

				foreach( var diagnostic in ctx.Diagnostics ) {
					var span = diagnostic.Location.SourceSpan;

					SyntaxNode syntax = root.FindNode( span, getInnermostNodeForTie: true );
					if( !( syntax is AwaitExpressionSyntax awaitExpression ) ) {
						continue;
					}

					ctx.RegisterCodeFix(
						CodeAction.Create(
							title: "Configure awaited task",
							ct => ConfigureAwaitedTask(
								orig: ctx.Document,
								root: root,
								awaitExpression: awaitExpression,
								useSafeAsync: useSafeAsync,
								cancellationToken: ct
							)
						),
						diagnostic
					);
				}
			}

			private static InvocationExpressionSyntax ConfigureTask( ExpressionSyntax task ) =>
				SyntaxFactory
					.InvocationExpression( SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						task,
						SyntaxFactory.IdentifierName( "ConfigureAwait" )
					) )
					.WithArgumentList( SyntaxFactory.ArgumentList( SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory
							.Argument( SyntaxFactory.LiteralExpression( SyntaxKind.FalseLiteralExpression ) )
							.WithNameColon( SyntaxFactory.NameColon( "continueOnCapturedContext" ) )
					) ) );

			private static InvocationExpressionSyntax SafeAsyncify( ExpressionSyntax task ) =>
				SyntaxFactory
					.InvocationExpression( SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						task,
						SyntaxFactory.IdentifierName( "SafeAsync" )
					) );

			private static Task<Document> ConfigureAwaitedTask(
				Document orig,
				SyntaxNode root,
				AwaitExpressionSyntax awaitExpression,
				bool useSafeAsync,
				CancellationToken cancellationToken
			) {
				ExpressionSyntax rhs = awaitExpression.Expression;
				InvocationExpressionSyntax replacement = useSafeAsync
					? SafeAsyncify( rhs )
					: ConfigureTask( rhs );

				root = root.ReplaceNode( rhs, replacement );

				var newDoc = orig.WithSyntaxRoot( root );

				return Task.FromResult( newDoc );
			}

			private static async Task<bool> CanReferenceSafeAsync(
				CodeFixContext ctx
			) {
				Compilation compilation = await ctx
					.Document
					.Project
					.GetCompilationAsync( ctx.CancellationToken )
					.ConfigureAwait( false );

				foreach( AssemblyIdentity reference in compilation.ReferencedAssemblyNames ) {
					if( reference.Name == "D2L.Core" ) {
						return true;
					}
				}

				return false;
			}
		}
	}
}
