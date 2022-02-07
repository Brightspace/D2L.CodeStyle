using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Generators.Config {

	internal static class GlobalConfigParser {

		public static GlobalConfig? TryParseGlobalConfig(
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
