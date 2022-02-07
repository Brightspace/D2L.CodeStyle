using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public static class AnalyzerDiagnosticsComparer {

		public static DiagnosticsComparison Compare(
				DiagnosticDescriptorCatalog descriptors,
				IEnumerable<Diagnostic> actualDiagnostics,
				IEnumerable<ExpectedDiagnostic> expectedDiagnostics
			) {

			ImmutableHashSet<ComputedDiagnostic> actual = GetComputedDiagnostics( descriptors, actualDiagnostics );
			ImmutableHashSet<ComputedDiagnostic> expected = GetComputedDiagnostics( descriptors, expectedDiagnostics );

			ImmutableHashSet<ComputedDiagnostic> matched = actual.Intersect( expected );
			ImmutableHashSet<ComputedDiagnostic> missing = expected.Except( matched );
			ImmutableHashSet<ComputedDiagnostic> unexpected = actual.Except( matched );

			return new DiagnosticsComparison(
				Matched: matched,
				Missing: missing,
				Unexpected: unexpected
			);
		}

		private static ImmutableHashSet<ComputedDiagnostic> GetComputedDiagnostics(
				DiagnosticDescriptorCatalog descriptors,
				IEnumerable<Diagnostic> actualDiagnostics
			) {

			var builder = ImmutableHashSet.CreateBuilder<ComputedDiagnostic>();

			foreach( Diagnostic diagnostic in actualDiagnostics ) {

				if( !descriptors.TryGetAlias( diagnostic.Id, out string? alias ) ) {
					alias = null;
				}

				LinePositionSpan linePosition = diagnostic.Location.GetLineSpan().Span;

				ComputedDiagnostic computed = new ComputedDiagnostic(
					Alias: alias,
					Id: diagnostic.Id,
					LinePosition: linePosition,
					Message: diagnostic.GetMessage()
				);

				builder.Add( computed );
			}

			return builder.ToImmutable();
		}

		private static ImmutableHashSet<ComputedDiagnostic> GetComputedDiagnostics(
				DiagnosticDescriptorCatalog descriptors,
				IEnumerable<ExpectedDiagnostic> expectedDiagnostics
			) {

			var builder = ImmutableHashSet.CreateBuilder<ComputedDiagnostic>();

			foreach( ExpectedDiagnostic expected in expectedDiagnostics ) {

				if( !descriptors.TryGetDescriptor( expected.Alias, out DiagnosticDescriptor? descriptor ) ) {
					throw new Exception( $"Failed to map diagnostic descriptor from alias '{ expected.Alias }'." );
				}

				string message = Diagnostic.Create(
						descriptor,
						null,
						messageArgs: expected.MessageArguments.ToArray<object>()
					)
					.GetMessage();

				ComputedDiagnostic computed = new ComputedDiagnostic(
					Alias: expected.Alias,
					Id: descriptor.Id,
					LinePosition: expected.LinePosition,
					Message: message
				);

				builder.Add( computed );
			}

			return builder.ToImmutable();
		}
	}
}
