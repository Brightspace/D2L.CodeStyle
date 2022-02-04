using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics {

	internal static class ReportDiagnosticExtensions {

		#region CompilationAnalysisContext

		public static void ReportDiagnostic(
				this CompilationAnalysisContext context,
				DiagnosticDescriptor descriptor,
				Location? location,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					location,
					messageArgs
				);
		}

		public static void ReportDiagnostic(
				this CompilationAnalysisContext context,
				DiagnosticDescriptor descriptor,
				ImmutableArray<Location> locations,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					locations,
					messageArgs
				);
		}

		#endregion

		#region OperationAnalysisContext

		public static void ReportDiagnostic(
				this OperationAnalysisContext context,
				DiagnosticDescriptor descriptor,
				Location? location,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					location,
					messageArgs
				);
		}

		public static void ReportDiagnostic(
				this OperationAnalysisContext context,
				DiagnosticDescriptor descriptor,
				ImmutableArray<Location> locations,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					locations,
					messageArgs
				);
		}

		#endregion

		#region SymbolAnalysisContext

		public static void ReportDiagnostic(
				this SymbolAnalysisContext context,
				DiagnosticDescriptor descriptor,
				Location? location,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					location,
					messageArgs
				);
		}

		public static void ReportDiagnostic(
				this SymbolAnalysisContext context,
				DiagnosticDescriptor descriptor,
				ImmutableArray<Location> locations,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					locations,
					messageArgs
				);
		}

		#endregion

		#region SyntaxNodeAnalysisContext

		public static void ReportDiagnostic(
				this SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				Location? location,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					location,
					messageArgs
				);
		}

		public static void ReportDiagnostic(
				this SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				ImmutableArray<Location> locations,
				params object?[]? messageArgs
			) {

			ReportDiagnostic(
					context.ReportDiagnostic,
					descriptor,
					locations,
					messageArgs
				);
		}

		#endregion

		private static void ReportDiagnostic(
				Action<Diagnostic> sink,
				DiagnosticDescriptor descriptor,
				Location? location,
				object?[]? messageArgs
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor,
					location,
					messageArgs
				);

			sink( diagnostic );
		}

		private static void ReportDiagnostic(
				Action<Diagnostic> sink,
				DiagnosticDescriptor descriptor,
				ImmutableArray<Location> locations,
				object?[]? messageArgs
			) {

			Location? location;
			IEnumerable<Location> additionalLocations;

			if( locations.IsEmpty ) {
				location = null;
				additionalLocations = Enumerable.Empty<Location>();

			} else {
				location = locations[ 0 ];
				additionalLocations = locations.Skip( 1 );
			}

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor,
					location,
					additionalLocations,
					messageArgs
				);

			sink( diagnostic );
		}
	}
}
