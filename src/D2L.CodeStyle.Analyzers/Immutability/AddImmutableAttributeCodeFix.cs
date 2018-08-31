using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( AddImmutableAttributeCodeFix )
	)]
	public sealed class AddImmutableAttributeCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.MissingTransitiveImmutableAttribute.Id
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

				var decl = root.FindNode( identifierSpan ) as TypeDeclarationSyntax;

				context.RegisterCodeFix(
					CodeAction.Create(
						title: "Add [Immutable]",
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

		// Syntax for "[Immutable]"
		private static readonly AttributeListSyntax ImmutableAttributeSyntax =
			SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Attribute(
						SyntaxFactory.IdentifierName( "Immutable" )
					)
				)
			);

		// Syntax for "using static D2L.CodeStyle.Annotations.Objects;"
		private static readonly UsingDirectiveSyntax TheNecessaryUsingDirective =
			SyntaxFactory.UsingDirective(
				SyntaxFactory.ParseName( "D2L.CodeStyle.Annotations.Objects" )
			).WithStaticKeyword(
				SyntaxFactory.Token( SyntaxKind.StaticKeyword )
			);

		private static Task<Document> Fix(
			Document orig,
			CompilationUnitSyntax root,
			TypeDeclarationSyntax decl,
			CancellationToken ct
		) {
			var newDecl = AddImmutableAttribute( decl );

			var newRoot = root.ReplaceNode( decl, newDecl );

			if ( !newRoot.Usings.Any( IsTheNecessaryUsingDirective ) ) {
				newRoot = newRoot.WithUsings(
					newRoot.Usings.Add( TheNecessaryUsingDirective )
				);
			}

			var newDoc = orig.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}

		public static bool IsTheNecessaryUsingDirective( UsingDirectiveSyntax u ) {
			if ( u.StaticKeyword == null ) {
				return false;
			}

			// Using a string here is pretty lame but I think it's ok.
			return u.Name.ToString() == "D2L.CodeStyle.Annotations.Objects";
		}
		
		public static TypeDeclarationSyntax AddImmutableAttribute(
			TypeDeclarationSyntax decl
		) {
			if ( decl is ClassDeclarationSyntax cls ) {
				return AddImmutableAttribute( cls );
			} else if ( decl is StructDeclarationSyntax st ) {
				return AddImmutableAttribute( st );
			} else {
				return AddImmutableAttribute( decl as InterfaceDeclarationSyntax );
			}
		}

		#region AddImmutableAttribute overloads

		// TypeDeclarationSyntax (the base class of these 3 types) doesn't
		// have WithAttributeLists... so we need to copy+paste some code.

		private static TypeDeclarationSyntax AddImmutableAttribute(
			ClassDeclarationSyntax decl
		) {
			return decl.WithAttributeLists(
				decl.AttributeLists.Add( ImmutableAttributeSyntax )
			);
		}

		private static TypeDeclarationSyntax AddImmutableAttribute(
			StructDeclarationSyntax decl
		) {
			return decl.WithAttributeLists(
				decl.AttributeLists.Add( ImmutableAttributeSyntax )
			);
		}

		private static TypeDeclarationSyntax AddImmutableAttribute(
			InterfaceDeclarationSyntax decl
		) {
			return decl.WithAttributeLists(
				decl.AttributeLists.Add( ImmutableAttributeSyntax )
			);
		}

		#endregion
	}
}
