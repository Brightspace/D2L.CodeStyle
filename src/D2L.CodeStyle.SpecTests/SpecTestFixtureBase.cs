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

			Compilation compilation = await GetCompilationForSourceAsync( m_test.Name, m_test.Source, m_test.MetadataReferences );
			CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)compilation.SyntaxTrees.First().GetRoot();

			m_actualDiagnostics = ( await GetActualDiagnosticsAsync( compilation, analyzer, m_test.AdditionalFiles ) )
				.Select( PrettyDiagnostic.Create )
				.ToImmutableArray();

			m_expectedDiagnostics = GetExpectedDiagnostics( compilationUnit, m_test.DiagnosticDescriptors )
				.Select( PrettyDiagnostic.Create )
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
			ImmutableDictionary<string, DiagnosticDescriptor> diagnosticDescriptors
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

					// The diagnostic must be between the two delimiting comments,
					// with one leading and trailing space inside the delimiters.
					// i.e.    /* Foo */ abcdef hijklmno pqr /**/
					//                   -------------------
					//                      ^ expected Foo diagnostic
					//
					// TODO: it would be nice to do fuzzier matching (e.g. ignore
					// leading and trailing whitespace inside delimiters.) 
					var diagStart = start.Trivia.GetLocation().SourceSpan.End + 1;
					var diagEnd = end.Trivia.GetLocation().SourceSpan.Start - 1;
					Assert.Less( diagStart, diagEnd );
					var diagSpan = TextSpan.FromBounds( diagStart, diagEnd );

					yield return Diagnostic.Create(
						descriptor: descriptor,
						location: Location.Create( root.SyntaxTree, diagSpan ),
						messageArgs: diagnosticExpectation.Arguments.ToArray()
					);
				}
			}
		}

		private static Task<Compilation> GetCompilationForSourceAsync(
			string specName,
			string source,
			ImmutableArray<MetadataReference> metadataReferences
		) {
			var projectId = ProjectId.CreateNewId( debugName: specName );
			var filename = specName + ".cs";
			var documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			var solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, specName, specName, LanguageNames.CSharp )
				.AddMetadataReferences( projectId, metadataReferences )
				.AddDocument( documentId, filename, SourceText.From( source ) );

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

		private sealed record PrettyDiagnostic(
			string Id,
			LinePositionSpan LinePosition,
			string Message
		) {

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
				WriteProperty( nameof( LinePosition ), LinePosition );
				WriteProperty( nameof( Message ), Message, seperator: string.Empty );
				sb.AppendLine( "}" );

				return sb.ToString();
			}

			public static PrettyDiagnostic Create( Diagnostic diagnostic ) {
				return new(
					Id: diagnostic.Id,
					LinePosition: diagnostic.Location.GetLineSpan().Span,
					Message: diagnostic.GetMessage()
				);
			}
		}
	}
}
