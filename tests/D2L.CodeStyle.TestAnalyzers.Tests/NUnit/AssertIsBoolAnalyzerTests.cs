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
							"Assert.Less( 3, 4 )", 
							"Assert.Greater( 3, 4 )" 
						),
					new Tuple<string, string, string>(
							"3 < 4, \"test message {{0}}\", \"with replacement\"",
							"Assert.Less( 3, 4, \"test message {0}\", \"with replacement\" )",
							"Assert.Greater( 3, 4, \"test message {0}\", \"with replacement\" )"
						),
					new Tuple<string, string, string>( 
							"3 <= 4", 
							"Assert.LessOrEqual( 3, 4 )", 
							"Assert.GreaterOrEqual( 3, 4 )" 
						),
					new Tuple<string, string, string>( 
							"3 > 4", 
							"Assert.Greater( 3, 4 )", 
							"Assert.Less( 3, 4 )" 
						),
					new Tuple<string, string, string>( 
							"3 >= 4", 
							"Assert.GreaterOrEqual( 3, 4 )", 
							"Assert.LessOrEqual( 3, 4 )" 
						),
					new Tuple<string, string, string>( 
							"3 == 4", 
							"Assert.AreEqual( 3, 4 )", 
							"Assert.AreNotEqual( 3, 4 )" 
						),
					new Tuple<string, string, string>(
							"3 == 4, \"test message\"",
							"Assert.AreEqual( 3, 4, \"test message\" )",
							"Assert.AreNotEqual( 3, 4, \"test message\" )"
						),
					new Tuple<string, string, string>( 
							"3 == null", 
							"Assert.IsNull( 3 )", 
							"Assert.IsNotNull( 3 )" 
						),
					new Tuple<string, string, string>( 
							"null == 3", 
							"Assert.IsNull( 3 )", 
							"Assert.IsNotNull( 3 )" 
						),
					new Tuple<string, string, string>(
							"null == 3, \"test message\"",
							"Assert.IsNull( 3, \"test message\" )",
							"Assert.IsNotNull( 3, \"test message\" )"
						),
					new Tuple<string, string, string>( 
							"3 is IEnumerable", 
							"Assert.IsInstanceOf<IEnumerable>( 3 )", 
							"Assert.IsNotInstanceOf<IEnumerable>( 3 )" 
						),
					new Tuple<string, string, string>(
							"3 is IEnumerable, \"test message {{0}}\", \"with replacement\"",
							"Assert.IsInstanceOf<IEnumerable>( 3, \"test message {0}\", \"with replacement\" )",
							"Assert.IsNotInstanceOf<IEnumerable>( 3, \"test message {0}\", \"with replacement\" )"
						),
				};

				const string isTrueSymbolName = "NUnit.Framework.Assert.IsTrue";
				const string isFalseSymbolName = "NUnit.Framework.Assert.IsFalse";

				TestCaseData GetDiagTestCase( string testCode, string symbolName, string expectedRecommendation ) =>
					new TestCaseData(
						GetCompleteTestClass( testCode ), new[] {
							new DiagnosticResult {
								Id = Diagnostics.MisusedAssertIsTrueOrFalse.Id,
								Message = string.Format( Diagnostics.MisusedAssertIsTrueOrFalse.MessageFormat.ToString(), symbolName, expectedRecommendation ),
								Severity = DiagnosticSeverity.Warning,
								Locations = new[] {
									new DiagnosticResultLocation( "Test0.cs", 8, 0 )
								}
							}
						}
					).SetName( testCode );

				foreach( var test in testCases ) {

					yield return GetDiagTestCase( $"Assert.IsTrue( {test.Item1} );", isTrueSymbolName, test.Item2 );

					yield return GetDiagTestCase( $"Assert.IsFalse( {test.Item1} );", isFalseSymbolName, test.Item3 );
				}

				// fqn test
				var fqnTest = testCases[0];
				yield return GetDiagTestCase( $"NUnit.Framework.Assert.IsTrue( {fqnTest.Item1} );", isTrueSymbolName, fqnTest.Item2 );
				yield return GetDiagTestCase( $"NUnit.Framework.Assert.IsFalse( {fqnTest.Item1} );", isFalseSymbolName, fqnTest.Item3 );
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
