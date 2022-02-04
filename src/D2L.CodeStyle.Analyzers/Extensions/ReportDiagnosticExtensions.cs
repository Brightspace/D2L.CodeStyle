using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis {

	internal static class ReportDiagnosticExtensions {

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
	}
}

namespace Microsoft.CodeAnalysis.Diagnostics {

	internal static class ReportDiagnosticExtensions {

		public static void ReportDiagnostic(
				this CompilationAnalysisContext context,
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

		public static void ReportDiagnostic(
				this OperationAnalysisContext context,
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

		public static void ReportDiagnostic(
				this SymbolAnalysisContext context,
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

		public static void ReportDiagnostic(
				this SyntaxNodeAnalysisContext context,
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
	}
}
