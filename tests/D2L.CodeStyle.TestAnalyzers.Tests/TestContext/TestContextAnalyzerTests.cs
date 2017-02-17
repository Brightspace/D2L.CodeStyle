using System.IO;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.TestContext {
	class TestContextAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new TestContextAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutTarget_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutTarget() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithTarget_Case1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithIgnore() {
				var status = TestContext.CurrentContext.Result.Status;  
				var state = TestContext.CurrentContext.Result.State;  

				if( TestContext.CurrentContext.Result.Status == TestStatus.Failed ) {

				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 9, 18 );
			var diag2 = CreateDiagnosticResult( 10, 17 );
			var diag3 = CreateDiagnosticResult( 12, 9 );
			VerifyCSharpDiagnostic( test, diag1, diag2, diag3 );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column ) {
			return new DiagnosticResult {
				Id = TestContextAnalyzer.DiagnosticId,
				Message = TestContextAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Warning,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
