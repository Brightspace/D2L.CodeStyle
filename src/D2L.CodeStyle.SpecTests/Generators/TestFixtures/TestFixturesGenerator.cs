using System.Collections.Immutable;
using System.Text.RegularExpressions;
using D2L.CodeStyle.SpecTests.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generators.TestFixtures {

	[Generator]
	public sealed class TestFixturesGenerator : IIncrementalGenerator {

		private sealed record class TestFixtureArgs(
			string IncludePath,
			string ProjectDirectory,
			string RootNamespace,
			string? Source
		);

		void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context ) {

			IncrementalValuesProvider<TestFixtureArgs> testFixtureArgs = context
				.AdditionalTextsProvider
				.Combine( context.AnalyzerConfigOptionsProvider )
				.Select( static ( (AdditionalText AdditionalText, AnalyzerConfigOptionsProvider OptionsProvider) source, CancellationToken cancellationToken ) => {

					AnalyzerConfigOptions options = source.OptionsProvider.GetOptions( source.AdditionalText );
					if( !options.IsAdditionalFileOfKind( "D2L.CodeStyle.SpecTest" ) ) {
						return null;
					}

					string includePath = source.AdditionalText.Path;
					string projectDirectory = options.GetRequiredOption( "build_property.projectdir" );
					string rootNamespace = options.GetRequiredOption( "build_property.rootnamespace" );

					string specSource;
					try {
						specSource = File.ReadAllText( includePath );
					} catch {
						return null;
					}

					return new TestFixtureArgs(
						IncludePath: includePath,
						ProjectDirectory: projectDirectory,
						RootNamespace: rootNamespace,
						Source: specSource
					);
				} )
				.WhereNotNull();

			context.RegisterImplementationSourceOutput( testFixtureArgs, GenerateTestFixture );
		}

		private static void GenerateTestFixture( SourceProductionContext context, TestFixtureArgs args ) {
			try {

				CancellationToken cancellationToken = context.CancellationToken;

				if( args.Source == null ) {

					// TODO: emit diagnostic
					return;
				}

				SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
						text: args.Source,
						path: args.IncludePath,
						cancellationToken: cancellationToken
					);

				AnalyzerSpec spec = AnalyzerSpecParser.Parse( syntaxTree, cancellationToken );

				string projectRelativePath = ProjectPathUtility.GetProjectRelativePath(
						args.ProjectDirectory,
						args.IncludePath
					);

				string specName = Path.GetFileNameWithoutExtension( args.IncludePath );

				string[] pathParts = projectRelativePath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

				string @namespace;
				if( pathParts.Length == 1 ) {
					@namespace = args.RootNamespace;
				} else {
					@namespace = string.Concat( args.RootNamespace, ".", string.Join( ".", pathParts, 0, pathParts.Length - 1 ) );
				}

				string[] classNames = Path
					.GetFileNameWithoutExtension( pathParts[ pathParts.Length - 1 ] )
					.Split( '.' );

				ImmutableArray<string> containerClassNames;
				if( classNames.Length == 1 ) {
					containerClassNames = ImmutableArray<string>.Empty;
				} else {
					containerClassNames = ImmutableArray.Create( classNames, 0, classNames.Length - 2 );
				}

				string fixtureClassName = classNames[ classNames.Length - 1 ];

				TestFixtureRenderer.Args renderArgs = new(
						Namespace: @namespace,
						ContainerClassNames: containerClassNames,
						FixtureClassName: fixtureClassName,
						Spec: spec,
						SpecName: specName,
						SpecSource: args.Source
					);

				string fixtureSource = TestFixtureRenderer.Render( renderArgs );
				string hintPath = GetHintPath( projectRelativePath );
				context.AddSource( hintPath, fixtureSource );

			} catch( Exception ex ) {
				context.ReportException( nameof( TestFixturesGenerator ), ex );
			}
		}

		private static readonly Regex m_invalidHintPathCharacters = new(
				@"[^\w.,\-+`_ ()\[\]{}]",
				RegexOptions.Compiled
			);

		private static string GetHintPath( string projectRelativePath ) {

			string hintPath = m_invalidHintPathCharacters
				.Replace( projectRelativePath, m => m.Value[ 0 ] switch {
					'_' => "__",
					_ => "_"
				} );

			return hintPath;
		}
	}
}
