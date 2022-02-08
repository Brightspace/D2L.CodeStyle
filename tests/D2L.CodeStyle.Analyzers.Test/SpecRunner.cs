﻿using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {

	[TestFixtureSource( typeof( SpecTestsProvider ), nameof( SpecTestsProvider.GetAll ) )]
	internal sealed class Spec {

		/// <summary>
		/// Compares diagnostics based on their Id and location
		/// </summary>
		private sealed class DiagnosticComparer : IEqualityComparer<Diagnostic> {
			public static readonly DiagnosticComparer Instance = new DiagnosticComparer();

			bool IEqualityComparer<Diagnostic>.Equals( Diagnostic x, Diagnostic y ) {
				return x.Id == y.Id
					&& x.Location == y.Location
					&& x.GetMessage() == y.GetMessage();
			}

			int IEqualityComparer<Diagnostic>.GetHashCode( Diagnostic diag ) {
				var hashCode = diag.Id.GetHashCode();
				hashCode = ( hashCode * 397 ) ^ diag.Location.GetHashCode();
				hashCode = ( hashCode * 397 ) ^ diag.GetMessage().GetHashCode();
				return hashCode;
			}
		}

		private readonly ImmutableArray<Diagnostic> m_expectedDiagnostics;
		private readonly ImmutableArray<Diagnostic> m_actualDiagnostics;
		private readonly ImmutableHashSet<Diagnostic> m_matchedDiagnostics;

		/// <summary>
		/// Each spec causes a single compilation to happen. This constructor
		/// extracts all the information needed for the assertions in the
		/// other test cases. It is called by NUnit due to the
		/// TestFixtureSource attribute.
		/// </summary>
		/// <param name="specName">
		/// The name of the spec to run (its source code is in m_specSource.)
		/// </param>
		public Spec( SpecTest test ) {

			var analyzer = GetAnalyzerNameFromSpec( test.Source );

			var compilation = GetCompilationForSource( test.Name, test.Source, test.MetadataReferences );

			m_actualDiagnostics = GetActualDiagnostics( compilation, analyzer, test.AdditionalFiles );

			m_expectedDiagnostics = GetExpectedDiagnostics(
				(CompilationUnitSyntax)compilation.SyntaxTrees.First().GetRoot(),
				test.DiagnosticDescriptors
			).ToImmutableArray();

			m_matchedDiagnostics = m_actualDiagnostics
				.Intersect(
					m_expectedDiagnostics,
					DiagnosticComparer.Instance
				).ToImmutableHashSet( DiagnosticComparer.Instance );
		}

		[Test]
		public void NoUnexpectedDiagnostics() {
			var unexpectedDiagnostics = m_actualDiagnostics
				.Where( d => !m_matchedDiagnostics.Contains( d ) );

			CollectionAssert.IsEmpty( unexpectedDiagnostics );
		}

		// TODO: investigate: can/should this be TestCaseSource? Maybe it'd be
		// cool, but there are some snags. Firstly, TestCaseSource needs to come
		// from a static and we don't have context about what spec we're running
		// in those. Additionally it's not obvious what to name each test case
		// (line/column number?)

		[Test]
		public void ExpectedDiagnostics() {
			var missingDiagnostics = m_expectedDiagnostics
				.Where( d => !m_matchedDiagnostics.Contains( d ) );

			CollectionAssert.IsEmpty( missingDiagnostics );
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

		private static ImmutableArray<Diagnostic> GetActualDiagnostics(
			Compilation compilation,
			DiagnosticAnalyzer analyzer,
			ImmutableArray<AdditionalText> additionalFiles
		) {

			return compilation
				.WithAnalyzers(
					analyzers: ImmutableArray.Create( analyzer ),
					options: new AnalyzerOptions( additionalFiles )
				)
				.GetAnalyzerDiagnosticsAsync().Result
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

		private static Compilation GetCompilationForSource(
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

			return solution.Projects.First()
				.GetCompilationAsync().Result;
		}
	}
}
