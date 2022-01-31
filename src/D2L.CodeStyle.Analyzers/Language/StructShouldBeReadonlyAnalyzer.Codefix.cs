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
	partial class StructShouldBeReadonlyAnalyzer {

		[ExportCodeFixProvider(
			LanguageNames.CSharp,
			Name = nameof( AddReadonlyModifierCodefix )
		)]
		public sealed class AddReadonlyModifierCodefix : CodeFixProvider {

			public override ImmutableArray<string> FixableDiagnosticIds
				=> ImmutableArray.Create( Diagnostics.StructShouldBeReadonly.Id );

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
					if( !( syntax is StructDeclarationSyntax declaration ) ) {
						continue;
					}

					ctx.RegisterCodeFix(
						CodeAction.Create(
							title: "Add readonly modifier",
							ct => AddReadonlyModifier(
								doc: ctx.Document,
								root: root,
								declaration: declaration,
								ct: ct
							)
						),
						diagnostic
					);
				}
			}

			private static readonly SyntaxToken READONLY_SYNTAX
				= SyntaxFactory.Token( SyntaxKind.ReadOnlyKeyword );
			private static Task<Document> AddReadonlyModifier(
				Document doc,
				SyntaxNode root,
				StructDeclarationSyntax declaration,
				CancellationToken ct
			) {
				SyntaxTokenList modifiers = declaration.Modifiers;
				int insertAt = GetModifierInsertLocation( modifiers );
				modifiers = modifiers.Insert( insertAt, READONLY_SYNTAX );

				root = root.ReplaceNode( declaration, declaration.WithModifiers( modifiers ) );
				doc = doc.WithSyntaxRoot( root );
				return Task.FromResult( doc );
			}

			private static int GetModifierInsertLocation(
				SyntaxTokenList modifiers
			) {
				for( int i = 0; i < modifiers.Count; ++i ) {
					SyntaxToken modifier = modifiers[i];

					switch( modifier.Kind() ) {
						case SyntaxKind.InternalKeyword:
						case SyntaxKind.PrivateKeyword:
						case SyntaxKind.PublicKeyword:
							return i + 1;
						case SyntaxKind.PartialKeyword:
						case SyntaxKind.RefKeyword:
							return i;
					}
				}

				return modifiers.Count;
			}
		}
	}
}
