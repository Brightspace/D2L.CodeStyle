using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {
	[TestFixtureSource(nameof(m_specNames))]
	internal sealed class Spec {
		private static readonly IEnumerable m_specNames;
		private static readonly ImmutableDictionary<string, string> m_specSource;

		private readonly ImmutableArray<Diagnostic> m_expectedDiagnostics;
		private readonly ImmutableArray<Diagnostic> m_actualDiagnostics;
		private readonly ImmutableHashSet<Diagnostic> m_unexpectedDiagnostics;

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
			m_expectedDiagnostics = GetExpectedDiagnostics( (CompilationUnitSyntax)compilation.SyntaxTrees.First().GetRoot() );

			// TODO: implement by diffing m_actualDiagnostics and
			// m_expectedDiagnostics. diagnostics should be matched up by start
			// location, end location if necessary and code if necessary. Prefer
			// to match up as little as necessary and assert equality for the
			// rest to provide best dev UX.
			// ---
			// This is done in the constructor because both the
			// NoUnexpectedDiagnostics and ExpectedDiagnostics tests use it.
			m_unexpectedDiagnostics = ImmutableHashSet<Diagnostic>.Empty;
		}

		[Test]
		public void NoUnexpectedDiagnostics() {
			CollectionAssert.IsEmpty( m_unexpectedDiagnostics );
		}

		// TODO: investigate: can/should this be TestCaseSource? Maybe it'd be
		// cool, but there are some snags. Firstly, TestCaseSource needs to come
		// from a static and we don't have context about what spec we're running
		// in those. Additionally it's not obvious what to name each test case
		// (line/column number?)

		[Test]
		public void ExpectedDiagnostics() {
			// TODO: this isn't the correct assertion. It is weaker than what
			// we should be asserting.
			Assert.AreEqual(
				m_expectedDiagnostics.Length,
				m_actualDiagnostics.Length
			);
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

				using( var specStream = new StreamReader( assembly.GetManifestResourceStream( specFilePath ) ) ) {
					source = specStream.ReadToEnd();
				}

				specNameToSourceCode[specName] = source;
			}
		}

		private DiagnosticAnalyzer GetAnalyzerNameFromSpec( string source ) {
			var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText( source ).GetRoot();

			string rawComment = root.GetLeadingTrivia()
				.First(
					t => t.Kind() == SyntaxKind.SingleLineCommentTrivia
					|| t.Kind() == SyntaxKind.MultiLineCommentTrivia )
				.ToString();

			var settings =
				Regex.Split( GetCommentContents( rawComment ), @"\r?\n" )
				.Select( l => {
					var parts = l.Split( new[] { ':' }, 2 );
					return new KeyValuePair<string, string>( parts[0].Trim(), parts[1].Trim() );
				} )
				.ToImmutableDictionary( StringComparer.InvariantCultureIgnoreCase );

			Assert.True( settings.ContainsKey( "analyzer" ), "spec must start with a comment of the form \"// analyzer: <type of analyzer to test>\"" );

			string analyzerName = settings["analyzer"];

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

		private static ImmutableArray<Diagnostic> GetExpectedDiagnostics( CompilationUnitSyntax root ) {
			// TODO: this is full of garbage, only important thing right now is its output length is correct
			return root.DescendantTrivia()
				.Where( t => t.Kind() == SyntaxKind.MultiLineCommentTrivia )
				.Select( c => GetCommentContents( c.ToString() ) )
				.Where( c => c != "" )
				.Select( c => Diagnostic.Create( Diagnostics.RpcContextFirstArgument, Location.None ) )
				.ToImmutableArray();
		}

		private static Compilation GetCompilationForSource( string specName, string source ) {
			var projectId = ProjectId.CreateNewId( debugName: specName );
			var filename = specName + ".cs";
			var documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			var solution = new AdhocWorkspace().CurrentSolution
				.AddProject( projectId, specName, specName, LanguageNames.CSharp )

				.AddMetadataReference(
					projectId,
					// mscorlinb
					MetadataReference
						.CreateFromFile( typeof( object ).Assembly.Location ) )

				.AddMetadataReference(
					projectId,
					// system.core
					MetadataReference
						.CreateFromFile( typeof( Enumerable ).Assembly.Location ) )

				.AddDocument( documentId, filename, SourceText.From( source ) );

			return solution.Projects.First()
				.GetCompilationAsync().Result;
		}

		private static string GetCommentContents( string rawComment ) {
			// real ghetto
			return Regex.Replace( rawComment.Trim(), @"^(//|/\*)(?<contents>[^*]*)(\*/)*$", "${contents}" ).Trim();
		}
	}
}
