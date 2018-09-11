using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( AddImmutableExceptionsCodeFix )
	)]
	public sealed class AddImmutableExceptionsCodeFix : CodeFixProvider {
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.ImmutableExceptionInheritanceIsInvalid.Id
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

			var model = await context.Document
				.GetSemanticModelAsync( context.CancellationToken )
				.ConfigureAwait( false );

			foreach( var diagnostic in context.Diagnostics ) {
				var attr = root.FindNode(
					diagnostic.Location.SourceSpan
				) as AttributeSyntax;

				var maxExceptionsAllowed = diagnostic
					.Properties[ImmutabilityExceptionInheritanceAnalyzer.CODE_FIX_DATA_KEY];

				context.RegisterCodeFix(
					CodeAction.Create(
						title: "Restrict unaudited reasons allowed by [Immutable] annotation",
						createChangedDocument: ct => Fix(
							context.Document,
							root,
							attr,
							maxExceptionsAllowed: maxExceptionsAllowed
						)
					),
					diagnostic
				);
			}
		}

		public static Task<Document> Fix(
			Document orig,
			SyntaxNode root,
			AttributeSyntax attr,
			string maxExceptionsAllowed
		) {
			var syntaxForExcepts = maxExceptionsAllowed
				.Split( ',' )
				.Select( GetSyntaxForExceptName )
				.ToImmutableArray();

			var expr = CombineReasons( syntaxForExcepts );

			// newAttr will always have fewer exceptions than attr: see the
			// note in the analyzer attached to maximalExceptions
			var newAttr = attr.WithArgumentList(
				SyntaxFactory.AttributeArgumentList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.AttributeArgument(
							expr
						).WithNameEquals( SyntaxFactory.NameEquals( "Except" ) )
					)
				)
			);

			var newRoot = root.ReplaceNode( attr, newAttr );

			var newDoc = orig.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}

		public static NameSyntax GetSyntaxForExceptName( string name ) {
			return SyntaxFactory.ParseName( "Except." + name );
		}

		private static readonly NameSyntax ExceptNone
			= SyntaxFactory.ParseName( "Except.None" );

		public static ExpressionSyntax CombineReasons( ImmutableArray<NameSyntax> values ) {
			if ( values.IsEmpty ) {
				return ExceptNone;
			}

			ExpressionSyntax result = values[0];

			// Construct something like BinOp(BinOp(A, op, B), op, C)
			// This is the layout that the parser would normally create for
			// "A | B | C".
			for( int idx = 1; idx < values.Length; idx++ ) {
				result = SyntaxFactory.BinaryExpression(
					SyntaxKind.BitwiseOrExpression,
					result,
					values[idx]
				);
			}

			return result;
		}
	}
}
