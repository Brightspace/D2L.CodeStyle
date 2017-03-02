﻿using D2L.CodeStyle.TestAnalyzers.ParallelizableTests;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.SourceAttribute {

	internal sealed class TestCaseSourceAttributeAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new TestCaseSourceAttributeAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutTestCaseSource_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutTestCaseSource() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithStatic_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			static object[] DivideCases =
			{
				new object[] { 12, 3, 4 },
				new object[] { 12, 2, 6 },
				new object[] { 12, 4, 3 }
			};

			[TestCaseSource( ""DivideCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			object[] DivideCases =
			{
				new object[] { 12, 3, 4 },
				new object[] { 12, 2, 6 },
				new object[] { 12, 4, 3 }
			};
			
			[Test, TestCaseSource( ""DivideCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}
		}
	}";
			AssertSingleDiagnostic( test, 13, 11, "DivideCases" );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private IEnumerable<TestCaseData> ValidCases {
				get
				{
					yield return new TestCaseData( 12, 3, 4 );
					yield return new TestCaseData( 12, 2, 6 );
				}
			}

			[TestCaseSource( ""ValidCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}
		}
	}";
			AssertSingleDiagnostic( test, 14, 5, "ValidCases" );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case3_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private IEnumerable<TestCaseData> GetCachePolicies() {
				return new TestCaseData[] { new TestCaseData( 12, 3, 4 ), new TestCaseData( 12, 2, 6 ) }; 
			}

			[TestCaseSource( nameof( GetCachePolicies ) )]
			public void DivideTest( int n, int d, int q ) {
			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 5, "GetCachePolicies" );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case4_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			static IEnumerable TestCases{}

			[Test, TestCaseSource( typeof( MyFactoryClass ), ""TestCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}
		}
		public class MyFactoryClass {
			public IEnumerable TestCases
			{
				get
				{
					yield return new TestCaseData( 12, 3 ).Returns( 4 );
					yield return new TestCaseData( 12, 2 ).Returns( 6 );
					yield return new TestCaseData( 12, 4 ).Returns( 3 );
					yield return new TestCaseData( 0, 0 )
					  .Throws( typeof( DivideByZeroException ) )
					  .SetName( ""DivideByZero"" )
					  .SetDescription( ""An exception is expected"" );
				}
			}
		}
	}";
			AssertSingleDiagnostic( test, 8, 11, "TestCases" );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case5_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			object[] DivideCases =
			{
				new object[] { 12, 3, 4 },
				new object[] { 12, 2, 6 },
				new object[] { 12, 4, 3 }
			};

			[Test, TestCaseSource( ""DivideCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}

			private IEnumerable<TestCaseData> ValidCases {
				get
				{
					yield return new TestCaseData( 12, 3, 4 );
					yield return new TestCaseData( 12, 2, 6 );
				}
			}

			[TestCaseSource( ""ValidCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 13, 11, "DivideCases" );
			var diag2 = CreateDiagnosticResult( 25, 5, "ValidCases" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithTestCaseSource_WithoutStatic_Case6_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test : BaseTest{

			[TestCaseSource( ""ValidCases"" )]
			public void DivideTest( int n, int d, int q ) {
			}
		}

		class BaseTest : BaseBaseTest{
			
		}

		class BaseBaseTest{
			private IEnumerable<TestCaseData> ValidCases {
				get
				{
					yield return new TestCaseData( 12, 3, 4 );
					yield return new TestCaseData( 12, 2, 6 );
				}
			}
		}
	}";
			AssertSingleDiagnostic( test, 7, 5, "ValidCases" );
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
				Id = TestCaseSourceAttributeAnalyzer.DiagnosticId,
				Message = string.Format( TestCaseSourceAttributeAnalyzer.MessageFormat, message ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
