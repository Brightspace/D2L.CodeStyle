using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace D2L.CodeStyle.SpecTests {

	public abstract class SpecTestFixtureBase {

		private readonly SpecTest m_test;
		private ImmutableArray<PrettyDiagnostic> m_expectedDiagnostics;
		private ImmutableArray<PrettyDiagnostic> m_actualDiagnostics;
		private ImmutableHashSet<PrettyDiagnostic> m_matchedDiagnostics;

		/// <summary>
		/// Parameterized test fixture
		/// </summary>
		/// <param name="test">Provided by the <see cref="TestFixtureSourceAttribute"/>.</param>
		public SpecTestFixtureBase( SpecTest test ) {
			m_test = test;
		}

		[OneTimeSetUp]
		public async Task OneTimeSetUp() {

			var analyzer = GetAnalyzerNameFromSpec( m_test.Source );

			SourceText sourceText = SourceText.From( m_test.Source, Encoding.UTF8 );
			Compilation compilation = await GetCompilationForSourceAsync( m_test.Name, sourceText, m_test.MetadataReferences );
			CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)compilation.SyntaxTrees.First().GetRoot();

			m_actualDiagnostics = ( await GetActualDiagnosticsAsync( compilation, analyzer, m_test.AdditionalFiles ) )
				.Select( d => PrettyDiagnostic.Create( d, sourceText )  )
				.ToImmutableArray();

			m_expectedDiagnostics = GetExpectedDiagnostics( compilationUnit, m_test.DiagnosticDescriptors, sourceText )
				.Select( d => PrettyDiagnostic.Create( d, sourceText ) )
				.ToImmutableArray();

			m_matchedDiagnostics = m_actualDiagnostics
				.Intersect( m_expectedDiagnostics )
				.ToImmutableHashSet();
		}

		[Test]
		public void NoUnexpectedDiagnostics() {

			IEnumerable<PrettyDiagnostic> unexpectedDiagnostics = m_actualDiagnostics
				.Where( d => !m_matchedDiagnostics.Contains( d ) );

			Assert.Multiple( () => {
				foreach( PrettyDiagnostic diagnostic in unexpectedDiagnostics ) {
					Assert.Fail( "An unexpected diagnostic was reported: {0}", diagnostic );
				}
			} );

			CollectionAssert.IsEmpty( unexpectedDiagnostics );
		}

		// TODO: investigate: can/should this be TestCaseSource? Maybe it'd be
		// cool, but there are some snags. Firstly, TestCaseSource needs to come
		// from a static and we don't have context about what spec we're running
		// in those. Additionally it's not obvious what to name each test case
		// (line/column number?)

		[Test]
		public void ExpectedDiagnostics() {

			IEnumerable<PrettyDiagnostic> missingDiagnostics = m_expectedDiagnostics
				.Where( d => !m_matchedDiagnostics.Contains( d ) );

			Assert.Multiple( () => {
				foreach( PrettyDiagnostic diagnostic in missingDiagnostics ) {
					Assert.Fail( "An expected diagnostic was not reported: {0}", diagnostic );
				}
			} );
		}

		private DiagnosticAnalyzer GetAnalyzerNameFromSpec( string source ) {
			var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText( source ).GetRoot();

			var header = root
				.GetLeadingTrivia()
				.First( t => t.Kind() == SyntaxKind.SingleLineCommentTrivia );

			Assert.NotNull( header, "There must be a single-line comment at the start of the file of the form \"analyzer: <type of analyzer to use in this spec>\"." );

			var headerContents = GetSingleLineCommentContents( header );

			StringAssert.StartsWith( "analyzer: ", headerContents, "First single-line comment must be of the form \"analyzer: <type of analyzer to use in this spec>\"." );

			string analyzerName = headerContents.Substring( "analyzer: ".Length ).Trim();

			var type = Type.GetType( analyzerName + ", D2L.CodeStyle.Analyzers" );

			Assert.NotNull( type, "couldn't get type for analyzer {0}", analyzerName );

			var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance( type );

			Assert.NotNull( analyzer, "couldn't instantiate type for analyzer {0}", analyzerName );

			return analyzer;
		}

		private async static Task<ImmutableArray<Diagnostic>> GetActualDiagnosticsAsync(
			Compilation compilation,
			DiagnosticAnalyzer analyzer,
			ImmutableArray<AdditionalText> additionalFiles
		) {

			ImmutableArray<Diagnostic> diagnostics = await compilation
				.WithAnalyzers(
					analyzers: ImmutableArray.Create( analyzer ),
					options: new AnalyzerOptions( additionalFiles )
				)
				.GetAnalyzerDiagnosticsAsync();

			return diagnostics
				.OrderBy( d => d.Location.SourceSpan )
				.ThenBy( d => d.Id )
				.ToImmutableArray();
		}

		private static IEnumerable<((SyntaxTrivia Trivia, string Content) Start, (SyntaxTrivia Trivia, string Content) End)> GroupCommentsIntoAdjacentPairs(
			IEnumerable<SyntaxTrivia> trivia
		) {
			var stack = new Stack<(SyntaxTrivia Trivia, string Content)>();

			using( var it = trivia.GetEnumerator() ) {
				while( it.MoveNext() ) {
					var current = it.Current;
					var content = GetMultiLineCommentContents( current );
					var node = (current, content);

					if( string.IsNullOrWhiteSpace( content ) ) {
						var start = stack.Pop();

						yield return (start, node);
						continue;
					}

					stack.Push( node );
				}
			}

			if( stack.Count != 0 ) {
				Assert.Fail( $"Unmatched end delimiters for comments at { string.Join( ",", stack.Select( n => n.Trivia.GetLocation() ) ) }." );
			}
		}

		private static string GetSingleLineCommentContents(
			SyntaxTrivia comment
		) {
			string contents = comment.ToString().Trim();

			StringAssert.StartsWith( "//", contents ); // should never fail
			contents = contents.Substring( 2 );

			return contents.Trim();
		}

		private static string GetMultiLineCommentContents(
			SyntaxTrivia comment
		) {
			string contents = comment.ToString().Trim();

			StringAssert.StartsWith( "/*", contents ); // should never fail
			contents = contents.Substring( 2 );

			StringAssert.EndsWith( "*/", contents ); // should never fail
			contents = contents.Substring( 0, contents.Length - 2 );

			return contents;
		}

		private class DiagnosticExpectation {
			public DiagnosticExpectation(
				string name,
				ImmutableArray<string> arguments
			) {
				Name = name;
				Arguments = arguments;
			}

			public static IEnumerable<DiagnosticExpectation> Parse( string commentContent ) {
				IEnumerable<string> expectations = commentContent.Split( '|' ).Select( s => s.Trim() );
				foreach( var str in expectations ) {
					var indexOfOpenParen = str.IndexOf( '(' );

					if( indexOfOpenParen == -1 ) {
						yield return new DiagnosticExpectation(
							name: str,
							arguments: ImmutableArray<string>.Empty
						);
						continue;
					}

					Assert.AreEqual( ')', str[ str.Length - 1 ] );

					var name = str.Substring( 0, indexOfOpenParen );

					var arguments = str.Substring(
						indexOfOpenParen + 1,
						str.Length - 2 - indexOfOpenParen
					);

					string RemoveSingleLeadingSpace( string s ) {
						if( string.IsNullOrEmpty( s ) ) {
							return s;
						}

						if( s[ 0 ] == ' ' ) {
							return s.Substring( 1 );
						}

						return s;
					}

					yield return new DiagnosticExpectation(
						name: name,
						arguments: Regex.Split( arguments, @"(?<!\\)," )
							.Select( arg => arg.Replace( "\\,", "," ) )
							.Select( RemoveSingleLeadingSpace )
							.ToImmutableArray()
					);
				}
			}

			public string Name { get; }
			public ImmutableArray<string> Arguments { get; }
		}

		private static IEnumerable<Diagnostic> GetExpectedDiagnostics(
			CompilationUnitSyntax root,
			ImmutableDictionary<string, DiagnosticDescriptor> diagnosticDescriptors,
			SourceText sourceText
		) {

			var multilineComments = root
				.DescendantTrivia()
				.Where( c => c.Kind() == SyntaxKind.MultiLineCommentTrivia );

			var commentPairs = GroupCommentsIntoAdjacentPairs(
				multilineComments
			);

			foreach( var comments in commentPairs ) {
				var start = comments.Start;
				var end = comments.End;

				var diagnosticExpectations = DiagnosticExpectation.Parse( start.Content );

				foreach( var diagnosticExpectation in diagnosticExpectations ) {
					// Every assertion's start comment must map to a diagnostic
					// defined in Common.Diagnostics
					DiagnosticDescriptor descriptor;
					if( !diagnosticDescriptors.TryGetValue( diagnosticExpectation.Name, out descriptor ) ) {
						Assert.Fail( $"Comment on line {start.Trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1} with {diagnosticExpectation.Name} doesn't map to a diagnostic defined in Common.Diagnostic" );

					}

					// The diagnostic must be between the two delimiting comments.
					// i.e.    /* Foo */ abcdef hijklmno pqr /**/
					//                   -------------------
					//                      ^ expected Foo diagnostic
					var searchStart = start.Trivia.Span.End;
					var searchEnd = end.Trivia.Span.Start;
					Assert.Less( searchStart, searchEnd );

					string source = sourceText.GetSubText(
						TextSpan.FromBounds( searchStart, searchEnd )
					).ToString();

					int leadingPadding = source.TakeWhile( char.IsWhiteSpace ).Count();
					int trailingPadding = source.Reverse().TakeWhile( char.IsWhiteSpace ).Count();

					TextSpan finalSpan = TextSpan.FromBounds(
						searchStart + leadingPadding,
						searchEnd - trailingPadding
					);

					yield return Diagnostic.Create(
						descriptor: descriptor,
						location: Location.Create( root.SyntaxTree, finalSpan ),
						messageArgs: diagnosticExpectation.Arguments.ToArray()
					);
				}
			}
		}

		private static Task<Compilation> GetCompilationForSourceAsync(
			string specName,
			SourceText source,
			ImmutableArray<MetadataReference> metadataReferences
		) {
			var projectId = ProjectId.CreateNewId( debugName: specName );
			var filename = specName + ".cs";
			var documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			var solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, specName, specName, LanguageNames.CSharp )
				.AddMetadataReferences( projectId, metadataReferences )
				.AddDocument( documentId, filename, source );

			var compilationOptions = solution
				.GetProject( projectId )
				.CompilationOptions
				.WithOutputKind( OutputKind.DynamicallyLinkedLibrary );

			CSharpParseOptions parseOptions = solution
				.GetProject( projectId )
				.ParseOptions as CSharpParseOptions;

			parseOptions = parseOptions
				.WithLanguageVersion( LanguageVersion.CSharp10 );

			solution = solution
				.WithProjectCompilationOptions( projectId, compilationOptions )
				.WithProjectParseOptions( projectId, parseOptions );

			return solution.Projects.First().GetCompilationAsync();
		}

		private sealed class PrettyDiagnostic {

			internal readonly record struct Line(
				int LineNumber,
				string Text
			);

			private PrettyDiagnostic(
				string id,
				LinePositionSpan linePosition,
				string message,
				ImmutableArray<Line> lines
			) {
				Id = id;
				LinePosition = linePosition;
				Message = message;
				Lines = lines;
			}

			public string Id { get; }
			public LinePositionSpan LinePosition { get; }
			public string Message { get; }
			public ImmutableArray<Line> Lines { get; }

			public override bool Equals( object obj ) {
				if( this is null ) {
					return obj is null;
				}

				if( obj is not PrettyDiagnostic other ) {
					return false;
				}

				return Id.Equals( other.Id )
					&& LinePosition.Equals( other.LinePosition )
					&& Message.Equals( other.Message, StringComparison.Ordinal );
			}

			public override int GetHashCode() => HashCode.Combine(
				Id,
				LinePosition,
				Message
			);

			/// <remarks>
			/// Formatting output for Visual Studio Test Explorer
			/// </remarks>
			public override string ToString() {

				StringBuilder sb = new StringBuilder();
				const string indent = "\t\t";

				void WriteProperty( string name, object value, string seperator = "," ) {
					sb.Append( indent );
					sb.Append( name );
					sb.Append( " = " );
					sb.Append( value );
					sb.AppendLine( seperator );
				}

				sb.AppendLine( "{" );
				WriteProperty( nameof( Id ), Id );
				WriteProperty( nameof( LinePosition ), $"({LinePosition.Start.Line + 1},{LinePosition.Start.Character})-({LinePosition.End.Line + 1},{LinePosition.End.Character})" );
				WriteProperty( nameof( Message ), Message, seperator: string.Empty );

				sb.AppendLine();

				WriteSourceReference( sb );

				sb.AppendLine( "}" );

				return sb.ToString();
			}

			private void WriteSourceReference( StringBuilder sb ) {
				int lineNumberWidth = CalculateLineNumberWidth( LinePosition );
				void WriteLineNumberPadding()
					=> sb.Append( ' ', repeatCount: lineNumberWidth + 2 );

				void WriteLineNumber( Line line ) {
					sb.AppendFormat( $"{{0,{lineNumberWidth}}}", line.LineNumber );
					sb.Append( ':' );
					sb.Append( ' ' );
				}

				int greatestCommonWhitespace = FindGreatestCommonWhitespaceCount( Lines );
				string GetTextIgnoringCommonWhitespace( Line line )
					=> greatestCommonWhitespace switch {
						0 => line.Text,
						_ => line.Text.Substring( greatestCommonWhitespace )
					};

				void WriteEqualWidthWhitespace( char source ) {
					char toAppend = source switch {
						'\t' => '\t',
						_ => ' '
					};
					sb.Append( toAppend );
				}

				void WriteBlankLineWithIndicatorsInPosition(
					Line line,
					char indicator,
					params int[] positions
				) {
					string referenceLine = GetTextIgnoringCommonWhitespace( line );

					// adjust positions to account for whitespace removal
					positions = positions.Select( p => p - greatestCommonWhitespace ).ToArray();

					for( int i = 0; i <= positions.Last(); ++i ) {
						if( positions.Contains( i ) ) {
							sb.Append( indicator );
							continue;
						}

						WriteEqualWidthWhitespace( referenceLine[ i ] );
					}

					sb.AppendLine();
				}

				// Multi-line diagnostic, draw leading indicator above source lines
				if( Lines.Length > 1 ) {
					WriteLineNumberPadding();
					WriteBlankLineWithIndicatorsInPosition(
						Lines[ 0 ],
						'↓',
						LinePosition.Start.Character
					);
				}

				// Write the actual source lines
				foreach( Line line in Lines ) {
					WriteLineNumber( line );
					sb.AppendLine( GetTextIgnoringCommonWhitespace( line ) );
				}

				// Draw indicator(s) below source lines
				WriteLineNumberPadding();

				int[] lastLineIndicatorPositions = Lines.Length > 1
					? new[] { LinePosition.End.Character - 1 }
					: new[] { LinePosition.Start.Character, LinePosition.End.Character - 1 };
				WriteBlankLineWithIndicatorsInPosition(
					Lines.Last(),
					'↑',
					lastLineIndicatorPositions
				);
			}

			public static PrettyDiagnostic Create( Diagnostic diagnostic, SourceText source ) {
				LinePositionSpan linePosition = diagnostic.Location.GetLineSpan().Span;
				ImmutableArray<Line> lines = source
					.Lines
					.Skip( linePosition.Start.Line )
					.Take( linePosition.End.Line - linePosition.Start.Line + 1 )
					.Select( l => new Line(
						LineNumber: l.LineNumber + 1,
						Text: source.GetSubText( l.Span ).ToString()
					) )
					.ToImmutableArray();

				return new(
					id: diagnostic.Id,
					linePosition: linePosition,
					message: diagnostic.GetMessage(),
					lines: lines
				);
			}

			private static int FindGreatestCommonWhitespaceCount(
				ImmutableArray<Line> lines
			) {
				int gcw = 0;

				Line referenceLine = lines[ 0 ];
				for( ; gcw < referenceLine.Text.Length; ++gcw ) {
					char c = referenceLine.Text[ gcw ];

					if( !char.IsWhiteSpace( c ) ) {
						return gcw;
					}

					foreach( Line line in lines.Skip( 1 ) ) {
						if( line.Text[ gcw ] != c ) {
							return gcw;
						}
					}
				}

				return gcw;
			}

			private static int CalculateLineNumberWidth( LinePositionSpan span )
				=> (int)Math.Floor( Math.Log10( span.End.Line + 1 ) + 1 );
		}
	}
}
