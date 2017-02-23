using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.Asserts {

	internal sealed class AssertsAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new AssertsAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutTargetAsserts_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutTargetAsserts() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithTargetAssertsCase1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithTargetAsserts() {
				string html=""tesT"";
				Assert.IsNotNullOrEmpty( html.ToLower(), ""message"" );
			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 5 );
		}

		[Test]
		public void DocumentWithTargetAssertsCase2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithTargetAsserts() {
				string html=""tesT"";
				Assert.IsNullOrEmpty( html.ToLower(), ""message"" );
			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 5 );
		}

		[Test]
		public void DocumentWithTargetAssertsCase3_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithTargetAsserts() {
				IDictionary<long, string> profileIds = new Dictionary<long, string>();
				profileIds.ForEach( entry => Assert.IsNotNullOrEmpty( entry.Value ) );
			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 34 );
		}

		[Test]
		public void DocumentWithTargetAssertsCase4_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithTargetAsserts() {
				List<String> ids = new List<string>();
				foreach( var id in ids ) {
					Assert.IsNullOrEmpty( id, ""message"" );
				}
			}
		}
	}";
			AssertSingleDiagnostic( test, 11, 6 );
		}

		[Test]
		public void DocumentWithTargetAssertsCase5_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithTargetAsserts() {
				string html=""tesT"";
				Assert.IsNotNullOrEmpty( html.ToLower() );
				Assert.IsNullOrEmpty( html );
				
				List<String> ids = new List<string>();
				foreach( var id in ids ) {
					Assert.IsNullOrEmpty( id, ""message"" );
				}
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 10, 5 );
			var diag2 = CreateDiagnosticResult( 11, 5 );
			var diag3 = CreateDiagnosticResult( 15, 6 );
			VerifyCSharpDiagnostic( test, diag1, diag2, diag3 );
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
				Id = AssertsAnalyzer.DiagnosticId,
				Message = AssertsAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
