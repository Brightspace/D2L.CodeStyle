using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {
	[TestFixtureSource(nameof(m_specNames))]
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

		private static readonly IEnumerable m_specNames;
		private static readonly ImmutableDictionary<string, string> m_specSource;
		private static readonly ImmutableDictionary<string, DiagnosticDescriptor> m_possibleDiagnostics;

		private readonly ImmutableArray<Diagnostic> m_expectedDiagnostics;
		private readonly ImmutableArray<Diagnostic> m_actualDiagnostics;
		private readonly ImmutableHashSet<Diagnostic> m_matchedDiagnostics;

		/// <summary>
		/// Loads all the source code to all the spec files.
		/// We need this to happen in the static constructor because
		/// TestFixtureSource needs this data to be available prior to fixture
		/// instantiation.
		/// Compilation doesn't happen here to avoid unexpected errors taking
		/// down all tests and better parallelizability.
		/// </summary>
		static Spec() {
			var builder = ImmutableDictionary.CreateBuilder<string, string>();

			LoadAllSpecSourceCode( builder );

			m_specSource = builder.ToImmutable();

			m_specNames = m_specSource.Keys;

			m_possibleDiagnostics = typeof( Diagnostics )
				.GetFields()
				.Select( f => new {
					f.Name,
					Value = f.GetValue( null ) as DiagnosticDescriptor
				} )
				.ToImmutableDictionary( nv => nv.Name, nv => nv.Value );

			foreach( var diag in m_possibleDiagnostics ) {
				Assert.NotNull( diag.Value, $"Common.Diagnostics.{diag.Key} must be of type DiagnosticDescriptor and not null" );
			}
		}

		/// <summary>
		/// Each spec causes a single compilation to happen. This constructor
		/// extracts all the information needed for the assertions in the
		/// other test cases. It is called by NUnit due to the
		/// TestFixtureSource attribute.
		/// </summary>
		/// <param name="specName">
		/// The name of the spec to run (its source code is in m_specSource.)
		/// </param>
		public Spec( string specName ) {
			var source = m_specSource[specName];

			var analyzer = GetAnalyzerNameFromSpec( source );

			var compilation = GetCompilationForSource( specName, source );

			m_actualDiagnostics = GetActualDiagnostics( compilation, analyzer );

			m_expectedDiagnostics = GetExpectedDiagnostics(
				(CompilationUnitSyntax)compilation.SyntaxTrees.First().GetRoot()
			).ToImmutableArray();

			m_matchedDiagnostics = m_actualDiagnostics
				.Intersect(
					m_expectedDiagnostics,
					DiagnosticComparer.Instance
				).ToImmutableHashSet();
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
				.Where( d => !m_matchedDiagnostics.Contains(
					d,
					DiagnosticComparer.Instance
				) );

			CollectionAssert.IsEmpty( missingDiagnostics );
		}

		/// <summary>
		/// Loads the source to all specs stored in resources into an IDictionary
		/// </summary>
		/// <param name="specNameToSourceCode">
		/// Dictionary to cache source in
		/// </param>
		private static void LoadAllSpecSourceCode(
			IDictionary<string, string> specNameToSourceCode
		) {
			var assembly = Assembly.GetExecutingAssembly();

			foreach( var specFilePath in assembly.GetManifestResourceNames() ) {
				if( !specFilePath.EndsWith( ".cs" ) ) {
					continue;
				}

				// The file foo/bar.baz.cs has specName bar.baz
				string specName = Regex.Replace(
					specFilePath,
					@"^.*\.(?<specName>[^\.]*)\.cs$",
					@"${specName}"
				);

				string source;
				using( var stream = assembly.GetManifestResourceStream( specFilePath ) )
				using( var specStream = new StreamReader( stream ) ) {
					source = specStream.ReadToEnd();
				}

				specNameToSourceCode[specName] = source;
			}
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

		private static ImmutableArray<Diagnostic> GetActualDiagnostics( Compilation compilation, DiagnosticAnalyzer analyzer ) {
			return compilation
				.WithAnalyzers( ImmutableArray.Create( analyzer ) )
				.GetAnalyzerDiagnosticsAsync().Result
				.ToImmutableArray();
		}

		private static IEnumerable<Tuple<SyntaxTrivia, SyntaxTrivia>> GroupCommentsIntoAdjacentPairs(
			IEnumerable<SyntaxTrivia> trivia
		) {
			using( var it = trivia.GetEnumerator() ) {
				while( it.MoveNext() ) {
					var first = it.Current;

					if ( !it.MoveNext() ) {
						Assert.Fail( $"Missing end delimiter for comment at {first.GetLocation()}" );
					}

					yield return new Tuple<SyntaxTrivia, SyntaxTrivia>(
						first,
						it.Current
					);
				}
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

			return contents.Trim();
		}

		private class DiagnosticExpectation {
			public DiagnosticExpectation(
				string name,
				ImmutableArray<string> arguments
			) {
				Name = name;
				Arguments = arguments;
			}

			public static DiagnosticExpectation Parse( string str ) {
				var indexOfOpenParen = str.IndexOf( '(' );

				if( indexOfOpenParen == -1 ) {
					return new DiagnosticExpectation(
						name: str,
						arguments: ImmutableArray<string>.Empty
					);
				}

				Assert.AreEqual( ')', str[str.Length - 1] );

				var name = str.Substring( 0, indexOfOpenParen );

				var arguments = str.Substring(
					indexOfOpenParen + 1,
					str.Length - 2 - indexOfOpenParen
				);

				return new DiagnosticExpectation(
					name: name,
					arguments: arguments.Split( ',' ).ToImmutableArray()
				);
			}

			public string Name { get; }
			public ImmutableArray<string> Arguments { get; }
		}

		private static IEnumerable<Diagnostic> GetExpectedDiagnostics( CompilationUnitSyntax root ) {
			var multilineComments = root
				.DescendantTrivia()
				.Where( c => c.Kind() == SyntaxKind.MultiLineCommentTrivia );

			var commentPairs = GroupCommentsIntoAdjacentPairs(
				multilineComments
			);

			foreach( var comments in commentPairs ) {
				var start = comments.Item1;
				var end = comments.Item2;

				var startContents = GetMultiLineCommentContents( start );
				var endContents = GetMultiLineCommentContents( end );

				var diagnosticExpectation = DiagnosticExpectation.Parse( startContents );

				// Every assertion's start comment must map to a diagnostic
				// defined in Common.Diagnostics
				DiagnosticDescriptor descriptor;
				if ( !m_possibleDiagnostics.TryGetValue( diagnosticExpectation.Name, out descriptor ) ) {
					Assert.Fail( $"Comment at {start.GetLocation()} doesn't map to a diagnostic defined in Common.Diagnostic" );
					
				}
				// Every assertion-comment pair starts with a comment
				// containing the name of a diagnostic and then an empty
				// (multiline) comment to mark the end of the diagnostic.
				// Note that this means that assertions cannot overlap.
				if( endContents != "" ) {
					Assert.Fail( $"Expected empty comment at {end.GetLocation()} to end comment at {start.GetLocation()}, got {end}" );
				}

				// The diagnostic must be between the two delimiting comments,
				// with one leading and trailing space inside the delimiters.
				// i.e.    /* Foo */ abcdef hijklmno pqr /**/
				//                   -------------------
				//                      ^ expected Foo diagnostic
				//
				// TODO: it would be nice to do fuzzier matching (e.g. ignore
				// leading and trailing whitespace inside delimiters.) 
				var diagStart = start.GetLocation().SourceSpan.End + 1;
				var diagEnd = end.GetLocation().SourceSpan.Start - 1;
				Assert.Less( diagStart, diagEnd );
				var diagSpan = TextSpan.FromBounds( diagStart, diagEnd );

				yield return Diagnostic.Create(
					descriptor: descriptor,
					location: Location.Create( root.SyntaxTree, diagSpan ),
					messageArgs: diagnosticExpectation.Arguments.ToArray()
				);
			}
		}

		private static Compilation GetCompilationForSource( string specName, string source ) {
			var projectId = ProjectId.CreateNewId( debugName: specName );
			var filename = specName + ".cs";
			var documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			var solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, specName, specName, LanguageNames.CSharp )

				// mscorlib
				.AddMetadataReference(
					projectId,
					MetadataReference
						.CreateFromFile( typeof( object ).Assembly.Location ) )

				// system.core
				.AddMetadataReference(
					projectId,
					MetadataReference
						.CreateFromFile( typeof( Enumerable ).Assembly.Location ) )

				.AddDocument( documentId, filename, SourceText.From( source ) );

			return solution.Projects.First()
				.GetCompilationAsync().Result;
		}
	}
}
