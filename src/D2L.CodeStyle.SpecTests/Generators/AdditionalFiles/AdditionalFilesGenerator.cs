using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generators.AdditionalFiles {

	[Generator]
	public sealed class AdditionalFilesGenerator : IIncrementalGenerator {

		private sealed record class AdditionalFileArgs(
			string IncludePath,
			string? Text,
			string VirtualPath
		);

		void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context ) {

			IncrementalValueProvider<ImmutableArray<AdditionalFileArgs>> additionalFileArgs = context
				.AdditionalTextsProvider
				.Combine( context.AnalyzerConfigOptionsProvider )
				.Select( static ( (AdditionalText AdditionalText, AnalyzerConfigOptionsProvider OptionsProvider) source, CancellationToken cancellationToken ) => {

					AnalyzerConfigOptions options = source.OptionsProvider.GetOptions( source.AdditionalText );
					if( !options.IsAdditionalFileOfKind( "D2L.CodeStyle.SpecTest.GlobalAdditionalFile" ) ) {
						return null;
					}

					string includePath = source.AdditionalText.Path;

					string? text;
					try {
						text = File.ReadAllText( includePath );
					} catch {
						text = null;
					}

					string virtualPath;
					if( options.TryGetValue( "build_metadata.AdditionalFiles.VirtualPath", out string? value ) ) {
						virtualPath = value;
					} else {
						string projectDirectory = options.GetRequiredOption( "build_property.projectdir" );
						virtualPath = ProjectPathUtility.GetProjectRelativePath( projectDirectory, includePath );
					}

					return new AdditionalFileArgs(
						IncludePath: includePath,
						Text: text,
						VirtualPath: virtualPath
					);
				} )
				.WhereNotNull()
				.Collect();

			context.RegisterImplementationSourceOutput( additionalFileArgs, Generate );
		}

		private static void Generate( SourceProductionContext context, ImmutableArray<AdditionalFileArgs> additionalFileArgs ) {

			try {
				var builder = ImmutableArray.CreateBuilder<AdditionalFileRenderer.AdditionalFile>();

				foreach( AdditionalFileArgs args in additionalFileArgs ) {

					if( args.Text == null ) {
						// TODO: emit diagnostic
						continue;
					}

					builder.Add( new(
						IncludePath: args.IncludePath,
						Text: args.Text,
						VirtualPath: args.VirtualPath
					) );
				}

				string source = AdditionalFileRenderer.Render( builder.ToImmutable() );
				context.AddSource( "GlobalAdditionalFiles.cs", source );

			} catch( Exception ex ) {
				context.ReportException( nameof( AdditionalFilesGenerator ), ex );
			}
		}
	}
}
