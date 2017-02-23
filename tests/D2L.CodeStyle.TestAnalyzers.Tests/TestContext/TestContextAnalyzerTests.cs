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
	using NUnit.Framework;

	namespace test {
		class Test {

			[Test]
			public void TestWithTarget() {
				var status = TestContext.CurrentContext.Result.Status;  
				var state = TestContext.CurrentContext.Result.State;  

				if( TestContext.CurrentContext.Result.Status == TestStatus.Failed ) {

				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 10, 18 );
			var diag2 = CreateDiagnosticResult( 11, 17 );
			var diag3 = CreateDiagnosticResult( 13, 9 );
			VerifyCSharpDiagnostic( test, diag1, diag2, diag3 );
		}

		[Test]
		public void DocumentWithTarget_Case2_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {

			[Test]
			public void TestWithTarget() {
				var currentContext = TestContext.CurrentContext;  
				var state = currentContext.Result.State;  

				var result = TestContext.CurrentContext.Result;  
				if( result.Status == TestStatus.Failed ) {

				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 11, 17 );
			var diag2 = CreateDiagnosticResult( 14, 9 );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column ) {
			return new DiagnosticResult {
				Id = TestContextAnalyzer.DiagnosticId,
				Message = TestContextAnalyzer.MessageFormat,
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
