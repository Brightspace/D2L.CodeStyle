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

namespace D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute {

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JsonParamBinderAnalyzerFixer) )]
	public class JsonParamBinderAnalyzerFixer : CodeFixProvider {

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync( CodeFixContext context ) {
			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan sourceSpan = diagnostic.Location.SourceSpan;

			SyntaxNode root = await context.Document.GetSyntaxRootAsync(
				                  context.CancellationToken
			                  ).ConfigureAwait( continueOnCapturedContext: false );

			AttributeSyntax oldAttribute = root.FindToken(sourceSpan.Start)
				.Parent.AncestorsAndSelf().OfType<AttributeSyntax>().First();

			context.RegisterCodeFix( 
				CodeAction.Create( 
					Diagnostics.ObsoleteJsonParamBinder.Title.ToString(),
					ct => SwapAttributes( context.Document, oldAttribute, ct)
				), 
				diagnostic
			);
		}

		private async Task<Document> SwapAttributes( 
			Document document, 
			AttributeSyntax oldAttribute, 
			CancellationToken ct 
		) {
			SyntaxNode root = await document.GetSyntaxRootAsync( ct )
				                  .ConfigureAwait( continueOnCapturedContext: false );

			AttributeSyntax newAttribute = SyntaxFactory.Attribute(
				SyntaxFactory.IdentifierName( "StrictJsonParamBinder" ) );

			SyntaxNode newRoot = root.ReplaceNode(oldAttribute, newAttribute );

			return document.WithSyntaxRoot( newRoot );
		}

		public override ImmutableArray<string> FixableDiagnosticIds =>
			ImmutableArray.Create<string>( Diagnostics.ObsoleteJsonParamBinder.Id );

	}
}
