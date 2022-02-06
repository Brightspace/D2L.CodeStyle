using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generator {

	[Generator]
	public sealed class TestFixturesGenerator : IIncrementalGenerator {

		private sealed record TestFixture(
			string HintPath,
			string Source
		);

		void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context ) {

			IncrementalValuesProvider<TestFixture> testFixtures = context
				.AdditionalTextsProvider
				.Combine( context.AnalyzerConfigOptionsProvider )
				.Select( static ( (AdditionalText AdditionalText, AnalyzerConfigOptionsProvider OptionsProvider) source, CancellationToken cancellationToken ) => {

					try {

						AnalyzerConfigOptions options = source.OptionsProvider.GetOptions( source.AdditionalText );
						if( !options.TryGetValue( "build_metadata.AdditionalFiles.Kind", out string? kind ) ) {
							return null;
						}

						if( !kind.Equals( "D2L.CodeStyle.SpecTest" ) ) {
							return null;
						}

						string GetRequiredOption( string key ) {

							if( !options.TryGetValue( key, out string? value ) ) {
								throw new InvalidOperationException( $"Could not get required '{ key }' option" );
							}

							return value;
						}

						string projectDirectory = GetRequiredOption( "build_property.projectdir" );
						string rootNamespace = GetRequiredOption( "build_property.rootnamespace" );

						string fullPath = source.AdditionalText.Path;
						string projectRelativePath = GetProjectRelativePath( projectDirectory, fullPath );
						AnalyzerSpec spec = AnalyzerSpecParser.Parse( fullPath, cancellationToken );

						TestFixture testFixture = RenderSpecTestFixture(
								projectRelativePath,
								rootNamespace,
								spec
							);

						return testFixture;

					} catch( Exception ex ) {
						Debugger.Launch();
						Console.WriteLine( ex );
						throw;
					}
				} )
				.WhereNotNull();

			context.RegisterSourceOutput( testFixtures, ( SourceProductionContext context, TestFixture testFixture ) => {
				context.AddSource( testFixture.HintPath, testFixture.Source );
			} );
		}

		private static TestFixture RenderSpecTestFixture(
				string projectRelativePath,
				string rootNamespace,
				AnalyzerSpec spec
			) {

			string[] pathParts = projectRelativePath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

			string @namespace;
			if( pathParts.Length == 1 ) {
				@namespace = rootNamespace;
			} else {
				@namespace = string.Concat( rootNamespace, ".", string.Join( ".", pathParts, 0, pathParts.Length - 1 ) );
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

			string fixtureSource = TestFixtureRenderer.Render(
					@namespace,
					containerClassNames,
					fixtureClassName,
					spec
				);

			string hintPath = GetHintPath( projectRelativePath );

			return new TestFixture(
				HintPath: hintPath,
				Source: fixtureSource
			);
		}

		private static string GetProjectRelativePath(
				string projectDirectory,
				string specTestPath
			) {

			// ensure santizied
			projectDirectory = Path.GetFullPath( projectDirectory );
			specTestPath = Path.GetFullPath( specTestPath );

			string projectRelativePath = specTestPath
				.Substring( projectDirectory.Length )
				.Trim( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

			return projectRelativePath;
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
