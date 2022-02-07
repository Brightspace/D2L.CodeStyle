using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Generators {

	internal static class SourceProductionContextExtensions {

		public static void ReportDiagnostic(
				this SourceProductionContext context,
				DiagnosticDescriptor descriptor,
				Location location,
				IEnumerable<Location>? additionalLocations = null,
				ImmutableDictionary<string, string?>? properties = null,
				object?[]? messageArgs = null
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: location,
					additionalLocations: additionalLocations,
					properties: properties,
					messageArgs: messageArgs
				);

			context.ReportDiagnostic( diagnostic );
		}

		public static void ReportException(
				this SourceProductionContext context,
				string generatorName,
				Exception exception
			) {

			Type exceptionType = exception.GetType();

			context.ReportDiagnostic(
					descriptor: Diagnostics.SourceGeneratorException,
					location: Location.None,
					messageArgs: new object[] {
						generatorName,
						exceptionType.FullName,
						exception.Message
					}
				);

			StringBuilder dump = new StringBuilder();
			dump.AppendLine( "/*" );
			dump.AppendLine( exception.ToString() );
			dump.AppendLine( "*/" );

			string hintName = $"{ exceptionType.Name }.{ Guid.NewGuid() }.cs";
			context.AddSource( hintName, dump.ToString() );
		}
	}
}
