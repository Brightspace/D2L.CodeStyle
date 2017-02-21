using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.ExpectedException {
	internal sealed class ExpectedExceptionAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ExpectedExceptionAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutException_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutException() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[ExpectedException( typeof( Exception ) )]
			public void TestWithException() {
			}

		}
	}";
			AssertSingleDiagnostic( test, 8, 5 );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[ExpectedException( ExpectedException = typeof( Exception ) )]
			public void TestWithException() {
			}

		}
	}";
			AssertSingleDiagnostic( test, 8, 5 );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase3_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[ExpectedException( typeof( Exception ), ExpectedMessage = ""Invalid orgId value"" )]
			public void TestWithException() {
			}

		}
	}";
			AssertSingleDiagnostic( test, 8, 5 );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase4_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[Test, ExpectedException( typeof( ArgumentException ) )]
			public void TestWithException() {
			}

		}
	}";
			AssertSingleDiagnostic( test, 8, 11 );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase5_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[TestCase( """", ExpectedException = typeof(ArgumentNullException), ExpectedMessage = ""Username"", MatchType=MessageMatch.Contains)]
			public void TestWithException(string name) {
			}

		}
	}";
			AssertSingleDiagnostic( test, 8, 19 );
		}

		[Test]
		public void DocumentWithExpectedExceptionCase6_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			[TestCase( """", ExpectedException = typeof(ArgumentNullException), ExpectedMessage = ""Username"", MatchType=MessageMatch.Contains)]
			[TestCase( ""name"", ExpectedException = typeof(ArgumentNullException), ExpectedMessage = ""Username"", MatchType=MessageMatch.Contains)]
			public void TestWithException(string name) {
			}

		}
	}";
			var diag1 = CreateDiagnosticResult( 8, 19 );
			var diag2 = CreateDiagnosticResult( 9, 23 );
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
				Id = ExpectedExceptionAnalyzer.DiagnosticId,
				Message = ExpectedExceptionAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Warning,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
