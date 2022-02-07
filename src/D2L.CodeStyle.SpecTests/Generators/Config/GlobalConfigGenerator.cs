using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

				GlobalConfig? config = TryParseGlobalConfigs(
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

		private static GlobalConfig? TryParseGlobalConfigs(
				SourceProductionContext context,
				string path,
				string xml
			) {

			XDocument doc;
			try {
				doc = XDocument.Parse( xml );
			} catch( XmlException ex ) {

				context.ReportDiagnostic(
					Diagnostics.GlobalConfigInvalid,
					location: Location.None,
					messageArgs: new[] {
						path,
						ex.Message
					}
				);
				return null;
			}

			ImmutableArray<string> diagnosticDescriptorSourceTypes = ParseDiagnosticDescriptorSourceTypes( context, path, doc );
			if( diagnosticDescriptorSourceTypes.IsEmpty ) {

				context.ReportDiagnostic(
						Diagnostics.GlobalConfigDiagnosticDescriptorSourceRequired,
						location: Location.None,
						messageArgs: new[] { path }
					);
			}

			ImmutableArray<string> referenceAssemblies = ParseReferenceAssemblies( context, path, doc );

			return new GlobalConfig(
				DiagnosticDescriptorSourceTypes: diagnosticDescriptorSourceTypes,
				ReferenceAssemblies: referenceAssemblies
			);
		}

		private static ImmutableArray<string> ParseDiagnosticDescriptorSourceTypes(
				SourceProductionContext context,
				string path,
				XDocument doc
			) {

			var diagnosticDescriptorSourceTypes = ImmutableArray.CreateBuilder<string>();

			IEnumerable<XElement> adds = doc.XPathSelectElements( "config/diagnosticDescriptorSources/add" );
			foreach( XElement add in adds ) {

				XAttribute? type = add.Attribute( "type" );
				if( type == null ) {

					context.ReportDiagnostic(
						Diagnostics.GlobalConfigInvalid,
						location: Location.None,
						messageArgs: new[] {
							path,
							"config/diagnosticDescriptorSources/add element missing 'type' attribute"
						}
					);

					continue;
				}

				diagnosticDescriptorSourceTypes.Add( type.Value );
			}

			return diagnosticDescriptorSourceTypes.ToImmutable();
		}

		private static ImmutableArray<string> ParseReferenceAssemblies(
				SourceProductionContext context,
				string path,
				XDocument doc
			) {

			var assemblies = ImmutableArray.CreateBuilder<string>();

			IEnumerable<XElement> adds = doc.XPathSelectElements( "config/references/add" );
			foreach( XElement add in adds ) {

				XAttribute? assembly = add.Attribute( "assembly" );
				if( assembly == null ) {

					context.ReportDiagnostic(
						Diagnostics.GlobalConfigInvalid,
						location: Location.None,
						messageArgs: new[] {
							path,
							"config/references/add element missing 'assembly' attribute"
						}
					);

					continue;
				}

				assemblies.Add( assembly.Value );
			}

			return assemblies.ToImmutable();
		}
	}
}
