#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.CommonFixes {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( RemoveAttributeCodeFix )
	)]
	public sealed class RemoveAttributeCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.AsyncMethodCannotBeBlocking.Id,
				Diagnostics.DontIntroduceBlockingInImplementation.Id,
				Diagnostics.UnnecessaryBlocking.Id
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

				var attr = root.FindNode( identifierSpan ) as AttributeSyntax;

				context.RegisterCodeFix(
					CodeAction.Create(
						title: $"Remove [{attr.Name}]",
						createChangedDocument: ct =>
							Task.FromResult( Fix( context.Document, root, attr ) )
					),
					diagnostic
				);
			}
		}

		private static Document Fix(
			Document orig,
			CompilationUnitSyntax root,
			AttributeSyntax attr
		) {
			var attrList = attr.Parent as AttributeListSyntax;

			if( attrList == null ) {
				// unexpected
				return orig;
			}

			CompilationUnitSyntax newRoot;

			if( attrList.Attributes.Count == 1 ) {
				// We're the only attribute in this list, so remove us
				// This keeps an extra newline that we don't really want, but avoids
				// deleting doc comment strings.
				newRoot = root.RemoveNode( attrList, SyntaxRemoveOptions.KeepExteriorTrivia );
			} else {
				// We're part of a list, e.g. removing b from [a, b, c]
				newRoot = root.RemoveNode( attr, SyntaxRemoveOptions.KeepNoTrivia );
			}

			var newDoc = orig.WithSyntaxRoot( newRoot );

			return newDoc;
		}
	}
}
