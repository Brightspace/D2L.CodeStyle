using System.IO;
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
	using NUnit.Framework;

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
	using NUnit.Framework;

	namespace test {
		class Test {
			private TestCaseData[] UserDetailsTestSource = {
				new TestCaseData(0,0 ).SetCategory("""").SetName( ""NoExistingResult_500Ids_Success"" ).Throws( typeof( Exception ) ),
				new TestCaseData( 0, 0 ).Throws( ""Exception"" ),
				new TestCaseData( 0, 0 )
			};
		}
	}";
			var diag1 = CreateDiagnosticResult( 8, 5, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			var diag2 = CreateDiagnosticResult( 9, 5, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase2_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {
			static IEnumerable GetTranslateTestCases() {
				yield return new TestCaseData( 0, 0 ).SetName( ""NoExistingResult_EmptyIdList_ExpectInvalidRequestDataException"" ).Throws( typeof( Exception ) );
				yield return new TestCaseData( 0, 500 ).SetCategory("""").Throws( ""Exception"" );
				yield return new TestCaseData( 0, 0 );
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 8, 18, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			var diag2 = CreateDiagnosticResult( 9, 18, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase3_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {
			private static TestCaseData[] CheckPassword_TestCases
			{
				get
				{
					return new TestCaseData[] {
						new TestCaseData(0,0 ).Throws( typeof( Exception ) ),
						new TestCaseData(0,0 ).Throws( ""Exception"" ),
						new TestCaseData(0,0 )
					};
				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 12, 7, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			var diag2 = CreateDiagnosticResult( 13, 7, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase4_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {
			private static IEnumerable<TestCaseData> CheckPassword_TestCases2
			{
				get
				{
					{
						TestCaseData test =new TestCaseData(0,0);
						test.SetCategory( """" );
						test.Throws( ""Exception"" );
						yield return test;
					}
					{
						TestCaseData test = new TestCaseData( 0, 0 );
						test.SetCategory( """" ).Throws( ""Exception"" );
						yield return test;
					}
				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 14, 7, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			var diag2 = CreateDiagnosticResult( 19, 7, "Use Assert.Throws or Assert.That in your test case instead of 'Throws'" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithThrowsCase5_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {
			private static IEnumerable<TestCaseData> CheckPassword_TestCases2
			{
				get
				{
					{
						TestCaseData test =new TestCaseData(0,0);
						test.SetCategory( """" );
						test.MakeExplicit( ""Bug: An unexpected exception type"" );
						yield return test;
					}
				}
			}
		}
	}";
			AssertSingleDiagnostic( test, 14, 7, "Do not use 'MakeExplicit'" );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column, string message ) {

			DiagnosticResult result = CreateDiagnosticResult( line, column, message );
			VerifyCSharpDiagnostic( file, result );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column, string message ) {
			return new DiagnosticResult {
				Id = TestCaseDataAnalyzer.DiagnosticId,
				Message = string.Format( TestCaseDataAnalyzer.MessageFormat, message ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}

		protected override MetadataReference[] GetAdditionalReferences() {
			return new MetadataReference[] { MetadataReference.CreateFromFile( Path.Combine(
				Path.GetDirectoryName( this.GetType().Assembly.Location ), @"..\..\..\..\packages\NUnit.2.6.4\lib\nunit.framework.dll"
			) ) };
		}
	}
}
