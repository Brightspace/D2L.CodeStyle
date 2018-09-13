using System.Collections.Immutable;
using System.Linq;
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
		Name = nameof( SealClassCodeFix )
	)]
	public sealed class SealClassCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.ClassShouldBeSealed.Id
			);

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(
			CodeFixContext context
		) {
			var root = await context.Document
				.GetSyntaxRootAsync( context.CancellationToken )
				.ConfigureAwait( false ) as CompilationUnitSyntax;

			foreach( var diagnostic in context.Diagnostics ) {
				var identifierSpan = diagnostic.Location.SourceSpan;

				var decl = root.FindNode( identifierSpan ) as ClassDeclarationSyntax;

				context.RegisterCodeFix(
					CodeAction.Create(
						title: "Seal class",
						createChangedDocument: ct => Fix(
							context.Document,
							root,
							decl,
							ct
						)
					),
					diagnostic
				);
			}
		}

		private static Task<Document> Fix(
			Document doc,
			CompilationUnitSyntax root,
			ClassDeclarationSyntax decl,
			CancellationToken ct
		) {
			var newModifiers = AddSealedToModifiers( decl.Modifiers );

			var newDecl = decl.WithModifiers( newModifiers );

			newDecl = RemoveUnnecessaryModifiers( newDecl );

			var newRoot = root.ReplaceNode( decl, newDecl );

			var newDoc = doc.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}

		private static readonly SyntaxToken SealedToken
			= SyntaxFactory.Token( SyntaxKind.SealedKeyword );

		private static SyntaxTokenList AddSealedToModifiers(
			SyntaxTokenList modifiers
		) {
			if ( modifiers.Count != 0 && modifiers.Last().Kind() == SyntaxKind.PartialKeyword ) {
				// Put "sealed" before partial, e.g. internal sealed partial
				return modifiers
					.RemoveAt( modifiers.Count - 1 )
					.Add( SealedToken )
					.Add( modifiers.Last() );
			}

			// Put "sealed" at the end of the modifiers (e.g internal sealed)
			return modifiers.Add( SealedToken );
		}

		private static ClassDeclarationSyntax RemoveUnnecessaryModifiers(
			ClassDeclarationSyntax decl
		) {
			var newDecl = decl;

			for( int i = 0; i < newDecl.Members.Count; i++ ) {
				var member = newDecl.Members[i];

				SyntaxTokenList newModifiers;
				MemberDeclarationSyntax newMember = null;

				// This is ugly because MemberDeclarationSyntax doesn't have
				// .Modifiers etc.

				if( member is PropertyDeclarationSyntax prop
				 && RemovedModifiers( prop.Modifiers, out newModifiers )
				) {
					newMember = prop.WithModifiers( newModifiers );

				} else if( member is MethodDeclarationSyntax method
						&& RemovedModifiers( method.Modifiers, out newModifiers )
				) {
					newMember = method.WithModifiers( newModifiers );

				} else if( member is IndexerDeclarationSyntax indexer
						&& RemovedModifiers( indexer.Modifiers, out newModifiers )
				) {
					newMember = indexer.WithModifiers( newModifiers );

				} else if( member is EventDeclarationSyntax ev
						&& RemovedModifiers( ev.Modifiers, out newModifiers )
				) {
					newMember = ev.WithModifiers( newModifiers );

				} else if ( member is FieldDeclarationSyntax field
					     && RemovedModifiers( field.Modifiers, out newModifiers )
				) {
					// fields can't be virtual but they can be protected
					newMember = field.WithModifiers( newModifiers );

				} else if ( member is ClassDeclarationSyntax cls
					     && RemovedModifiers( cls.Modifiers, out newModifiers )
				) {
					// classes can't be virtual but they can be protected
					newMember = cls.WithModifiers( newModifiers );

				} else if ( member is StructDeclarationSyntax st
					     && RemovedModifiers( st.Modifiers, out newModifiers )
				) {
					// structs can't be virtual but they can be protected
					newMember = st.WithModifiers( newModifiers );

				} else if ( member is EnumDeclarationSyntax en
					     && RemovedModifiers( en.Modifiers, out newModifiers )
				) {
					// enums can't be virtual but they can be protected
					newMember = en.WithModifiers( newModifiers );

				} else if ( member is InterfaceDeclarationSyntax iface
					     && RemovedModifiers( iface.Modifiers, out newModifiers )
				) {
					// enums can't be virtual but they can be protected
					newMember = iface.WithModifiers( newModifiers );

				} else if ( member is DelegateDeclarationSyntax del
					     && RemovedModifiers( del.Modifiers, out newModifiers )
				) {
					// enums can't be virtual but they can be protected
					newMember = del.WithModifiers( newModifiers );
				}

				if( newMember != null ) {
					newMember = newMember.WithLeadingTrivia( member.GetLeadingTrivia() );
					newDecl = newDecl.ReplaceNode( member, newMember );
				}
			}

			return newDecl;
		}

		private static bool RemovedModifiers(
			SyntaxTokenList modifiers,
			out SyntaxTokenList newModifiers
		) {
			newModifiers = modifiers;

			if ( modifiers.Count == 0 ) {
				return false;
			}

			var virtualModifier = modifiers
				.FirstOrDefault( m => m.Kind() == SyntaxKind.VirtualKeyword );

			if ( virtualModifier != null ) {
				newModifiers = modifiers.Remove( virtualModifier );
			}

			var protectedModifier = newModifiers
				.FirstOrDefault( m => m.Kind() == SyntaxKind.ProtectedKeyword );

			if ( protectedModifier != null ) {
				newModifiers = newModifiers.Remove( protectedModifier );
			}

			return modifiers != newModifiers;
		}
	}
}