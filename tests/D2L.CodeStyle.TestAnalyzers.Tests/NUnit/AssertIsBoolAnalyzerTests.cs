using System;
using System.Collections.Generic;
using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.Tests.NUnit {

	[TestFixture]
	internal sealed class AssertIsBoolAnalyzerTests : DiagnosticVerifier {

		/// <summary>
		/// "nameof" is an InvocationExpressionSyntax, but it is not represented in the semantic model (i.e. the symbol is null).
		/// Make sure that it does not make the analyzer crash with a NullReferenceException.
		/// </summary>
		[Test]
		public void NoSymbol_NoDiag() {

			const string test = @"
using NUnit.Framework;
namespace Test {
	[TestFixture]
	class Test {
		[Test]
		public void Test() {

			int testVar = 1;

			Assert.AreNotEqual( ""test"", nameof( testVar ) );
		}
	}
}";

			VerifyCSharpDiagnostic( test );
		}

		[TestCaseSource( nameof( NoDiagnosticTestCases ) )]
		public void NoDiag( string test ) {

			VerifyCSharpDiagnostic( test );
		}

		private static IEnumerable<TestCaseData> NoDiagnosticTestCases {
			get {
				const string testCodeFormat = @"
using NUnit.Framework;
namespace Test {{
	[TestFixture]
	class Test {{

		private bool GetVal() {{
			return 3 < 4;
		}}

		[Test]
		public void Test() {{

			// bool value
			Assert.{0}( true );

			// bool identifier
			bool val = 3 < 4; 
			Assert.{0}( val );

			// bool method invocation
			Assert.{0}( GetVal() );

			// unrelated assert
			Assert.AreEqual( 3, 4 );
		}}
	}}
}}";

				yield return new TestCaseData( string.Format( testCodeFormat, "IsTrue" ) ).SetName( "IsTrue" );
				yield return new TestCaseData( string.Format( testCodeFormat, "IsFalse" ) ).SetName( "IsFalse" );

				const string emptyTest = @"
using NUnit.Framework;
namespace Test {{
	[TestFixture]
	class Test {{
		[Test]
		public void Test() {{
		}}
	}}
}}";
				yield return new TestCaseData( emptyTest ).SetName( "empty test" );
			}
		}

		[TestCaseSource( nameof( DiagnosticTestCases ) )]
		public void WithDiag( string test, DiagnosticResult[] expectedDiagnostics ) {

			VerifyCSharpDiagnostic( test, expectedDiagnostics );
		}

		private static IEnumerable<TestCaseData> DiagnosticTestCases {
			get {

				const string isTrueSymbolName = "NUnit.Framework.Assert.IsTrue";
				const string isFalseSymbolName = "NUnit.Framework.Assert.IsFalse";

				string GetCompleteTestClass( string testCode, params object[] args ) => string.Format( @"
using NUnit.Framework;
namespace Test {{
	[TestFixture]
	class Test {{
		[Test]
		public void Test() {{
" + testCode + @"
		}}
	}}
}}", args );

				Tuple<string, string, string>[] testCases = {
					new Tuple<string, string, string>( 
							"3 < 4", 
							"Assert.Less", 
							"Assert.Greater" 
						),
					new Tuple<string, string, string>(
							"3 < 4, \"test message {{0}}\", \"with replacement\"",
							"Assert.Less",
							"Assert.Greater"
						),
					new Tuple<string, string, string>( 
							"3 <= 4", 
							"Assert.LessOrEqual", 
							"Assert.GreaterOrEqual" 
						),
					new Tuple<string, string, string>( 
							"3 > 4", 
							"Assert.Greater", 
							"Assert.Less" 
						),
					new Tuple<string, string, string>( 
							"3 >= 4", 
							"Assert.GreaterOrEqual", 
							"Assert.LessOrEqual" 
						),
					new Tuple<string, string, string>( 
							"3 == 4", 
							"Assert.AreEqual", 
							"Assert.AreNotEqual" 
						),
					new Tuple<string, string, string>(
							"3 == 4, \"test message\"",
							"Assert.AreEqual",
							"Assert.AreNotEqual"
						),
					new Tuple<string, string, string>( 
							"3 == null", 
							"Assert.IsNull", 
							"Assert.IsNotNull" 
						),
					new Tuple<string, string, string>( 
							"null == 3", 
							"Assert.IsNull", 
							"Assert.IsNotNull" 
						),
					new Tuple<string, string, string>(
							"null == 3, \"test message\"",
							"Assert.IsNull",
							"Assert.IsNotNull"
						),
					new Tuple<string, string, string>(
							"3 == 0",
							"Assert.Zero",
							"Assert.NotZero"
						),
					new Tuple<string, string, string>(
							"0 == 3",
							"Assert.Zero",
							"Assert.NotZero"
						),
					new Tuple<string, string, string>(
							"null == 0",
							"Assert.IsNull",
							"Assert.IsNotNull"
						),
					new Tuple<string, string, string>(
							"0 == null",
							"Assert.IsNull",
							"Assert.IsNotNull"
						),
					new Tuple<string, string, string>(
							"3 == true",
							"Assert.IsTrue",
							"Assert.IsFalse"
						),
					new Tuple<string, string, string>(
							"true == 3",
							"Assert.IsTrue",
							"Assert.IsFalse"
						),
					new Tuple<string, string, string>(
							"3 == false",
							"Assert.IsFalse",
							"Assert.IsTrue"
						),
					new Tuple<string, string, string>(
							"false == 3",
							"Assert.IsFalse",
							"Assert.IsTrue"
						),
					new Tuple<string, string, string>( 
							"3 is IEnumerable",
							"Assert.IsInstanceOf<IEnumerable>",
							"Assert.IsNotInstanceOf<IEnumerable>"
						),
					new Tuple<string, string, string>(
							"3 is IEnumerable, \"test message {{0}}\", \"with replacement\"",
							"Assert.IsInstanceOf<IEnumerable>",
							"Assert.IsNotInstanceOf<IEnumerable>"
						),
					new Tuple<string, string, string>(
							"3 is IList<string>",
							"Assert.IsInstanceOf<IList<string>>",
							"Assert.IsNotInstanceOf<IList<string>>"
						),
				};

				DiagnosticResult GetExpectedDiagnostic( string symbolName, string expectedRecommendation, int lineNumber = 8, int columnNumber = 0 ) {

					string message = string.Format( 
							Diagnostics.MisusedAssertIsTrueOrFalse.MessageFormat.ToString(), 
							symbolName, 
							expectedRecommendation 
						);

					return new DiagnosticResult {
						Id = Diagnostics.MisusedAssertIsTrueOrFalse.Id,
						Message = message,
						Severity = DiagnosticSeverity.Warning,
						Locations = new[] {
								new DiagnosticResultLocation( "Test0.cs", lineNumber, columnNumber )
							}
					};
				}

				TestCaseData GetDiagTestCase( string testCode, params DiagnosticResult[] expecDiagnosticResults ) =>
					new TestCaseData(
							GetCompleteTestClass( testCode ), 
							expecDiagnosticResults
						).SetName( testCode.Trim() );

				foreach( var test in testCases ) {

					yield return GetDiagTestCase( 
							$"Assert.IsTrue( {test.Item1} );", 
							GetExpectedDiagnostic( isTrueSymbolName, test.Item2 ) 
						);
					yield return GetDiagTestCase( 
							$"Assert.IsFalse( {test.Item1} );", 
							GetExpectedDiagnostic( isFalseSymbolName, test.Item3 ) 
						);
				}

				// fqn test
				var fqnTest = testCases[0];
				yield return GetDiagTestCase( 
						$"NUnit.Framework.Assert.IsTrue( {fqnTest.Item1} );", 
						GetExpectedDiagnostic( isTrueSymbolName, $"NUnit.Framework.{fqnTest.Item2}" ) 
					);
				yield return GetDiagTestCase( 
						$"NUnit.Framework.Assert.IsFalse( {fqnTest.Item1} );", 
						GetExpectedDiagnostic( isFalseSymbolName, $"NUnit.Framework.{fqnTest.Item3}" ) 
					);

				// trivia
				yield return GetDiagTestCase( $@"
					/*some test comment*/ Assert.IsTrue(
							{fqnTest.Item1},
							""test message""
						);",
					GetExpectedDiagnostic( isTrueSymbolName, fqnTest.Item2, lineNumber: 9, columnNumber: 28 )
				);

				// multiple diagnostic messages
				var testLine1 = $"NUnit.Framework.Assert.IsTrue( {testCases[ 0 ].Item1} );";
				var testLine2 = $"NUnit.Framework.Assert.IsFalse( {testCases[ 1 ].Item1} );";
				yield return GetDiagTestCase(
						$"{testLine1}{Environment.NewLine}{testLine2}",
						GetExpectedDiagnostic( isTrueSymbolName, $"NUnit.Framework.{testCases[ 0 ].Item2}", lineNumber: 8 ),
						GetExpectedDiagnostic( isFalseSymbolName, $"NUnit.Framework.{testCases[ 1 ].Item3}", lineNumber: 9 )
					);

				// alias test
				yield return new TestCaseData(
					@"
using NunitAssert = NUnit.Framework.Assert;
namespace Test {
	[TestFixture]
	class Test {
		[Test]
		public void Test() {
			NunitAssert.IsTrue( 3 < 4 );
		}
	}
}",
					new[] { GetExpectedDiagnostic( isTrueSymbolName, "NunitAssert.Less", columnNumber: 4 ) }
				).SetName( "alias test" );
			}
		}

		private static readonly MetadataReference NUnitReference = MetadataReference.CreateFromFile( 
				typeof( Assert ).Assembly.Location 
			);

		protected override MetadataReference[] GetAdditionalReferences() => new[] {
				NUnitReference
			};

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new AssertIsBoolAnalyzer();
		}
	}
}
