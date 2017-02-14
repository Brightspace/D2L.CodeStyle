using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.TestCaseData {

	internal sealed class TestCaseDataAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new TestCaseDataAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutThrows_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutThrows() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithThrowsCase1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private TestCaseData[] UserDetailsTestSource = {
				new TestCaseData(0,0 ).SetCategory('').SetName( 'NoExistingResult_500Ids_Success' ).Throws( typeof( Exception ) ),
				new TestCaseData( 0, 0 ).Throws( 'Exception' ),
				new TestCaseData( 0, 0 )
			};
		}
	}";
			var diag1 = CreateDiagnosticResult( 7, 89 );
			var diag2 = CreateDiagnosticResult( 8, 30 );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			static IEnumerable GetTranslateTestCases() {
				yield return new TestCaseData( 0, 0 ).SetName( 'NoExistingResult_EmptyIdList_ExpectInvalidRequestDataException' ).Throws( typeof( Exception ) );
				yield return new TestCaseData( 0, 500 ).SetCategory('').Throws( 'Exception' );
				yield return new TestCaseData( 0, 0 );
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 7, 119 );
			var diag2 = CreateDiagnosticResult( 8, 61 );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase3_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private static TestCaseData[] CheckPassword_TestCases
			{
				get
				{
					return new TestCaseData[] {
						new TestCaseData(0,0 ).Throws( typeof( Exception ) ),
						new TestCaseData(0,0 ).Throws( 'Exception' ),
						new TestCaseData(0,0 )
					};
				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 11, 30 );
			var diag2 = CreateDiagnosticResult( 12, 30 );
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
				Id = TestCaseDataAnalyzer.DiagnosticId,
				Message = TestCaseDataAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Warning,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
