using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using D2L.CodeStyle.Analyzers.Common;

namespace D2L.CodeStyle.Analyzers.UnsafeSingletons {

	internal sealed class UnsafeSingletonsAnalyzerTests : DiagnosticVerifier {
		private static readonly MutabilityInspectionResultFormatter s_formatter = new MutabilityInspectionResultFormatter();

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new UnsafeSingletonsAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			AssertNoDiagnostic( test );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic(
			string file,
			int line,
			int column,
			string typeName, MutabilityInspectionResult inspectionResult
		) {
			DiagnosticResult result = CreateDiagnosticResult( line, column, typeName, inspectionResult );
			VerifyCSharpDiagnostic( file, result );
		}

		private static DiagnosticResult CreateDiagnosticResult(
			int line,
			int column,
			string typeName,
			MutabilityInspectionResult result
		) {
			var reason = s_formatter.Format( result );

			return new DiagnosticResult {
				Id = UnsafeSingletonsAnalyzer.DiagnosticId,
				Message = string.Format( UnsafeSingletonsAnalyzer.MessageFormat, typeName, reason ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
