using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {
	[TestFixture]
	internal sealed class SpecRunner {
		[Test, TestCaseSource(typeof(SpecRunner), nameof(TestCases))]
		public async Task RunSpec( string testName, string source ) {
			CompilationUnitSyntax root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText( source ).GetRoot();

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

			Assert.True( settings.ContainsKey( "analyzer" ) );

			var type = Type.GetType( settings["analyzer"] + ", D2L.CodeStyle.Analyzers" );

			Assert.NotNull( type );

			var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance( type );

			Assert.NotNull( analyzer );

			var projectName = "SpecTestsFor" + testName;
			var filename = testName + ".cs";

			var projectId = ProjectId.CreateNewId( debugName: projectName );
			var documentId = DocumentId.CreateNewId( projectId, debugName: filename );

			var solution = new AdhocWorkspace()
				.CurrentSolution
				.AddProject( projectId, projectName, projectName, LanguageNames.CSharp )
				.AddMetadataReference( projectId, MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ) ) // mscorlib
				.AddMetadataReference( projectId, MetadataReference.CreateFromFile( typeof( Enumerable ).Assembly.Location ) ) // system.core
				.AddDocument( documentId, filename, SourceText.From( source ) );

			var compilation = ( await solution.Projects.First().GetCompilationAsync() )
				.WithAnalyzers( ImmutableArray.Create( analyzer ));

			var diags = (await compilation.GetAnalyzerDiagnosticsAsync()).ToImmutableArray();

			var multilineComments = root.DescendantTrivia()
				.Where( t => t.Kind() == SyntaxKind.MultiLineCommentTrivia )
				.Select( c => GetCommentContents( c.ToString() ) )
				.Where( c => c != "" ).ToImmutableArray();

			Assert.AreEqual( multilineComments.Length, diags.Length );
		}

		private static string GetCommentContents( string rawComment ) {
			// real ghetto
			return Regex.Replace( rawComment.Trim(), @"^(//|/\*)(?<contents>[^*]*)(\*/)*$", "${contents}" ).Trim();
		}

		public static IEnumerable TestCases {
			get {
				var resourceSet = Specs.SpecTests.ResourceManager.GetResourceSet( CultureInfo.CurrentUICulture, true, true );
				foreach ( DictionaryEntry resource in resourceSet ) {
					var testName = resource.Key.ToString();
					var source = resource.Value;

					yield return new TestCaseData(
						testName, source
					).SetCategory( "Unit" ).SetName( testName );
				}
			}
		}
	}
}
