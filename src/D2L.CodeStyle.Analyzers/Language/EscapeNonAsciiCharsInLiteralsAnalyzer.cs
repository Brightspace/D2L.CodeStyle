using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	// We are doing this because we (unfortunately) have a mix of encoding for
	// our source code files, and the encoding of the file impacts how strings
	// are interpreted. We really ought to clean that up, but until that
	// happens we can be safer if we avoid non-ASCII characters in our
	// literals. Automated refactoring has bit us before with this.
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class EscapeNonAsciiCharsInLiteralsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.EscapeNonAsciiCharsInLiteral );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(
				CheckForUnicodeLiterals,
				SyntaxKind.StringLiteralExpression
			);

			context.RegisterSyntaxNodeAction(
				CheckForUnicodeLiterals,
				SyntaxKind.CharacterLiteralExpression
			);
		}

		private static void CheckForUnicodeLiterals(
			SyntaxNodeAnalysisContext ctx
		) {
			// the node could be something like:
			//   "foo"
			//   @"foo"
			//   'x'
			var literalExpr = (LiteralExpressionSyntax)ctx.Node;

			// We can't handle verbatim strings for the same reason as these
			// folks: https://github.com/dotnet/codeformatter/issues/39 (TODO)
			if ( literalExpr.Token.IsVerbatimStringLiteral() ) {
				return;
			}

			string token = literalExpr.Token.Text;
			string escapedToken = null;

			if( StrictlyAscii( token, out escapedToken ) ) {
				return;
			}

			bool isChar =
				literalExpr.Kind() == SyntaxKind.CharacterLiteralExpression;

			var fixProps = ImmutableDictionary.CreateBuilder<string, string>();
			fixProps[EscapeCharCodeFix.ESCAPED] = escapedToken;

			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.EscapeNonAsciiCharsInLiteral,
					literalExpr.GetLocation(),
					fixProps.ToImmutable(),
					isChar ? "char" : "string",
					escapedToken
				)
			);
		}

		private static bool StrictlyAscii(
			string token,
			out string escapedToken
		) {
			var sb = new StringBuilder();
			var copyStartIdx = 0;

			// invariant: copyStartIdx < idx
			// Note: the enclosing quotes are included in val; don't bother looking at them.
			for( int idx = 1; idx < token.Length - 1; idx++ ) {
				if( token[idx] < 0x80 ) {
					continue;
				}

				// copy all the ascii chars we've seen between the last copy
				// and now (not inclusive) into sb
				sb.Append( token, copyStartIdx, idx - copyStartIdx );

				// next time we'll start copying from the char after us
				// (unless this happens again next loop)
				copyStartIdx = idx + 1;

				if ( IsSurrogatePair( token, idx ) ) {
					sb.AppendFormat(
						@"\U{0:X8}",
						char.ConvertToUtf32( token[idx], token[idx + 1] )
					);
				} else {
					sb.AppendFormat(
						@"\u{0:X4}",
						(ushort)token[idx]
					);
				}
			}

			// if copyStartIdx never changed we never saw non-ascii chars and
			// sb is empty.
			if ( copyStartIdx == 0 ) {
				escapedToken = null;
				return true;
			}

			// copy trailing ascii into sb. This is never a no-op because we
			// need to at least copy the ending quote.
			sb.Append( token, copyStartIdx, token.Length - copyStartIdx );

			escapedToken = sb.ToString();
			return false;
		}

		private static bool IsSurrogatePair( string str, int idx ) {
			return idx + 1 < str.Length
				&& char.IsHighSurrogate( str[idx] )
				&& char.IsLowSurrogate( str[idx + 1] );
		}
	}

	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( EscapeCharCodeFix )
	)]
	public sealed class EscapeCharCodeFix : CodeFixProvider {
		public const string ESCAPED = "escaped";

		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.EscapeNonAsciiCharsInLiteral.Id
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

				var literal = root.FindNode( span, getInnermostNodeForTie: true )
					as LiteralExpressionSyntax;

				if ( literal == null ) {
					continue;
				}

				var escapedToken = diagnostic.Properties[ESCAPED];

				ctx.RegisterCodeFix(
					CodeAction.Create(
						title: "Escape literal",
						ct => Fix( ctx.Document, root, literal, escapedToken ),
						equivalenceKey: nameof( EscapeCharCodeFix )
					),
					diagnostic
				);
			}
		}

		private static Task<Document> Fix(
			Document doc,
			SyntaxNode root,
			LiteralExpressionSyntax literal,
			string escapedToken
		) {
			var comment = SyntaxFactory
				.Comment( $"/* unencoded: {literal.Token.Text} */" );

			var newLiteral = literal.WithToken(
				SyntaxFactory.ParseToken( escapedToken )
			).WithLeadingTrivia(
				literal.GetLeadingTrivia()
					.Add( comment )
					.Add( SyntaxFactory.Whitespace( " " ) )
			);

			var newRoot = root.ReplaceNode( literal, newLiteral );

			var newDoc = doc.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}
	}
}
