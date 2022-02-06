using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static D2L.CodeStyle.SpecTests.Generator.AnalyzerSpec;

namespace D2L.CodeStyle.SpecTests.Generator {

	internal static class AnalyzerSpecParser {

		private static readonly Regex m_messageArgsRegex = new Regex( @"(?<!\\),", RegexOptions.Compiled );

		public static AnalyzerSpec Parse( string path, CancellationToken cancellationToken ) {

			string source = File.ReadAllText( path );

			SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
				text: source,
				path: path
			);

			CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot( cancellationToken );

			string analyzerQualifiedTypeName = ParseAnalyzerQualifiedTypeName( root );
			ImmutableArray<ExpectedDiagnostic> expectedDiagnostics = GetExpectedDiagnostics( root );

			return new AnalyzerSpec(
				analyzerQualifiedTypeName,
				expectedDiagnostics,
				source
			);
		}

		private static string ParseAnalyzerQualifiedTypeName( CompilationUnitSyntax root ) {

			SyntaxTrivia header = root
				.GetLeadingTrivia()
				.FirstOrDefault( t => t.IsKind( SyntaxKind.SingleLineCommentTrivia ) );

			if( header == default ) {
				throw new FormatException( "There must be a single-line comment at the start of the file of the form \"analyzer: <type of analyzer to use in this spec>\"." );
			}

			string headerContents = GetSingleLineCommentContents( header );
			if( !headerContents.StartsWith( "analyzer: " ) ) {
				throw new FormatException( "First single-line comment must be of the form \"analyzer: <type of analyzer to use in this spec>\"." );
			}

			string analyzerName = headerContents.Substring( "analyzer: ".Length ).Trim();
			if( analyzerName.Length == 0 ) {
				throw new FormatException( "Analyzer qualified type name cannot be empty." );
			}

			return analyzerName;
		}

		private static ImmutableArray<ExpectedDiagnostic> GetExpectedDiagnostics( CompilationUnitSyntax root ) {

			var builder = ImmutableArray.CreateBuilder<ExpectedDiagnostic>();

			IEnumerable<SyntaxTrivia> multilineComments = root
				.DescendantTrivia()
				.Where( c => c.IsKind( SyntaxKind.MultiLineCommentTrivia ) );

			IEnumerable<(TriviaAndContent Start, TriviaAndContent End)> commentPairs =
				GroupCommentsIntoAdjacentPairs( multilineComments );

			foreach( (TriviaAndContent start, TriviaAndContent end) in commentPairs ) {

				IEnumerable<NameAndMessageArgs> diagnostics = ParseDiagnosticNameAndMessageArgs( start.Content );
				foreach( NameAndMessageArgs diagnostic in diagnostics ) {

					// The diagnostic must be between the two delimiting comments,
					// with one leading and trailing space inside the delimiters.
					// i.e.    /* Foo */ abcdef hijklmno pqr /**/
					//                   -------------------
					//                      ^ expected Foo diagnostic
					//
					// TODO: it would be nice to do fuzzier matching (e.g. ignore
					// leading and trailing whitespace inside delimiters.) 
					int diagnosticStart = start.Trivia.GetLocation().SourceSpan.End + 1;
					int diagnosticEnd = end.Trivia.GetLocation().SourceSpan.Start - 1;
					if( diagnosticStart >= diagnosticEnd ) {
						throw new InvalidOperationException( "Diagnostic start should be before diagnostic end" );
					}

					TextSpan diagnosticSpan = TextSpan.FromBounds( diagnosticStart, diagnosticEnd );

					ExpectedDiagnostic expectedDiagnostic = new(
						Name: diagnostic.Name,
						Location: Location.Create( root.SyntaxTree, diagnosticSpan ),
						MessageArguments: diagnostic.MessageArgs
					);

					builder.Add( expectedDiagnostic );
				}
			}

			return builder.ToImmutable();
		}

		private readonly record struct TriviaAndContent(
			SyntaxTrivia Trivia,
			string Content
		);

		private static IEnumerable<(TriviaAndContent Start, TriviaAndContent End)> GroupCommentsIntoAdjacentPairs(
				IEnumerable<SyntaxTrivia> trivia
			) {

			Stack<TriviaAndContent> stack = new Stack<TriviaAndContent>();

			using( IEnumerator<SyntaxTrivia> it = trivia.GetEnumerator() ) {
				while( it.MoveNext() ) {
					SyntaxTrivia current = it.Current;
					string content = GetMultiLineCommentContents( current );
					TriviaAndContent node = new( current, content );

					if( string.IsNullOrWhiteSpace( content ) ) {
						TriviaAndContent start = stack.Pop();

						yield return (start, node);
						continue;
					}

					stack.Push( node );
				}
			}

			if( stack.Count != 0 ) {

				string messsage = $"Unmatched end delimiters for comments at { string.Join( ",", stack.Select( n => n.Trivia.GetLocation() ) ) }.";
				throw new FormatException( messsage );
			}
		}

		private static string GetSingleLineCommentContents( SyntaxTrivia comment ) {

			string contents = comment.ToString().Trim();

			if( !contents.StartsWith( "//" ) ) {
				throw new FormatException( "Single-line comment did star start with '//'." );
			}

			contents = contents.Substring( 2 ).Trim();
			return contents;
		}

		private static string GetMultiLineCommentContents( SyntaxTrivia comment ) {

			string contents = comment.ToString().Trim();

			if( !contents.StartsWith( "/*" ) ) {
				throw new FormatException( "Multi-line comment did not start with '/*'." );
			}

			if( !contents.EndsWith( "*/" ) ) {
				throw new FormatException( "Multi-line comment did not end with '*/'." );
			}

			contents = contents.Substring( 2, contents.Length - 4 );
			return contents;
		}

		private readonly record struct NameAndMessageArgs(
			string Name,
			ImmutableArray<string> MessageArgs
		);

		private static IEnumerable<NameAndMessageArgs> ParseDiagnosticNameAndMessageArgs( string commentContent ) {

			IEnumerable<string> expectations = commentContent.Split( '|' ).Select( s => s.Trim() );
			foreach( string str in expectations ) {

				int indexOfOpenParen = str.IndexOf( '(' );
				if( indexOfOpenParen == -1 ) {

					yield return new NameAndMessageArgs( str, ImmutableArray<string>.Empty );
					continue;
				}

				if( str[ str.Length - 1 ] != ')' ) {
					throw new FormatException( "Diagnostic expectation did not end in ')'." );
				}

				string name = str.Substring( 0, indexOfOpenParen );

				string arguments = str.Substring(
					indexOfOpenParen + 1,
					str.Length - 2 - indexOfOpenParen
				);

				ImmutableArray<string> messageArgs = m_messageArgsRegex
					.Split( arguments )
					.Select( arg => arg.Replace( "\\,", "," ) )
					.Select( RemoveSingleLeadingSpace )
					.ToImmutableArray();

				yield return new NameAndMessageArgs( name, messageArgs );
			}
		}

		private static string RemoveSingleLeadingSpace( string s ) {

			if( string.IsNullOrEmpty( s ) ) {
				return s;
			}

			if( s[ 0 ] == ' ' ) {
				return s.Substring( 1 );
			}

			return s;
		}
	}
}
