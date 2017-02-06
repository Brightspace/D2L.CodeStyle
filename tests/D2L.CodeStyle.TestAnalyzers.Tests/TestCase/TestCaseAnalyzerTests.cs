using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.TestCase {
	internal sealed class TestCaseAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new TestCaseAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutResult_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutResult() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithResultCase1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[TestCase( 1, Result = 2 )]
			public int TestWithResult( int i ) {
				return i + 1;
			}

			[TestCase( 1, ExpectedResult = 2 )]
			public int TestWithExpectedResult( int i ) {
				return i + 1;
			}
		}
	}";
			AssertSingleDiagnostic( test, 7, 18 );
		}

		[Test]
		public void DocumentWithResultCase2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[TestCase( 1, Result = 2 )]
			public int TestWithResult( int i ) {
				return i + 1;
			}

			[TestCase( 1, 2, Result = 3 )]
			public int TestWithResult2( int i, int j ) {
				return i + j;
			}

		}
	}";
			var diag1 = CreateDiagnosticResult( 7, 18 );
			var diag2 = CreateDiagnosticResult( 12, 21 );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column ) {

			DiagnosticResult result = CreateDiagnosticResult( line, column );
			VerifyCSharpDiagnostic( file, result );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column ) {
			return new DiagnosticResult {
				Id = TestCaseAnalyzer.DiagnosticId,
				Message = TestCaseAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Warning,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}

	}
}
