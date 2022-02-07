using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.SpecTests.Generators.Config {

	[Generator]
	public sealed class GlobalConfigGenerator : IIncrementalGenerator {

		private sealed record class GlobalConfigFile( string Path, string? Xml );

		void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context ) {

			IncrementalValueProvider<ImmutableArray<GlobalConfigFile>> configFiles = context
				.AdditionalTextsProvider
				.Combine( context.AnalyzerConfigOptionsProvider )
				.Select( static ( (AdditionalText AdditionalText, AnalyzerConfigOptionsProvider OptionsProvider) source, CancellationToken cancellationToken ) => {

					AnalyzerConfigOptions options = source.OptionsProvider.GetOptions( source.AdditionalText );
					if( !options.IsAdditionalFileOfKind( "D2L.CodeStyle.SpecTest.GlobalConfig" ) ) {
						return null;
					}

					string includePath = source.AdditionalText.Path;

					string? xml;
					try {
						xml = File.ReadAllText( includePath );
					} catch {
						xml = null;
					}

					return new GlobalConfigFile( includePath, xml );
				} )
				.WhereNotNull()
				.Collect();

			context.RegisterImplementationSourceOutput( configFiles, Generate );
		}

		private static void Generate(
				SourceProductionContext context,
				ImmutableArray<GlobalConfigFile> configFiles
			) {

			try {
				if( configFiles.IsEmpty ) {
					context.ReportDiagnostic(
							Diagnostics.NoGlobalConfigFile,
							Location.None
						);
					return;
				}

				if( configFiles.Length > 1 ) {
					context.ReportDiagnostic(
							Diagnostics.TooManyGlobalConfigFiles,
							Location.None
						);
					return;
				}

				GlobalConfigFile configFile = configFiles[ 0 ];
				if( configFile.Xml == null ) {
					context.ReportDiagnostic(
							Diagnostics.GlobalConfigFileNotFound,
							Location.None,
							messageArgs: new[] { configFile.Path }
						);
					return;
				}

				GlobalConfig? config = GlobalConfigParser.TryParseGlobalConfig(
						context,
						path: configFile.Path,
						xml: configFile.Xml
					);
				if( config == null ) {
					return;
				}

				string source = GlobalConfigRenderer.Render( config );
				context.AddSource( "GlobalConfig.cs", source );

			} catch( Exception ex ) {
				context.ReportException( nameof( GlobalConfigGenerator ), ex );
			}
		}
	}
}
