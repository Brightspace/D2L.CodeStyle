using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generators.AdditionalFiles {

	[Generator]
	public sealed class AdditionalFilesGenerator : IIncrementalGenerator {

		void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context ) {

			IncrementalValueProvider<ImmutableArray<AdditionalFile>> additionalFiles = context
				.AdditionalTextsProvider
				.Combine( context.AnalyzerConfigOptionsProvider )
				.Select( static ( (AdditionalText AdditionalText, AnalyzerConfigOptionsProvider OptionsProvider) source, CancellationToken cancellationToken ) => {

					AnalyzerConfigOptions options = source.OptionsProvider.GetOptions( source.AdditionalText );
					if( !options.IsAdditionalFileOfKind( "D2L.CodeStyle.SpecTest.GlobalAdditionalFile" ) ) {
						return null;
					}

					string includePath = source.AdditionalText.Path;

					string virtualPath;
					if( options.TryGetValue( "build_metadata.AdditionalFiles.VirtualPath", out string? value ) ) {
						virtualPath = value;
					} else {
						string projectDirectory = options.GetRequiredOption( "build_property.projectdir" );
						virtualPath = ProjectPathUtility.GetProjectRelativePath( projectDirectory, includePath );
					}

					return new AdditionalFile(
						OriginalPath: includePath,
						VirtualPath: virtualPath
					);
				} )
				.WhereNotNull()
				.Collect();

			context.RegisterSourceOutput( additionalFiles, Generate );
		}

		private static void Generate( SourceProductionContext context, ImmutableArray<AdditionalFile> additionalFiles ) {

			string source = AdditionalFilesRenderer.Render( additionalFiles );
			context.AddSource( "GlobalAdditionalFiles.cs", source );
		}
	}
}
