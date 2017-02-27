using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.IgnoreAttribute {

	internal sealed class IgnoreAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new IgnoreAttributeAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutIgnore_NoDiag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutIgnore() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutTestIgnore_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		public class IgnoreAttribute : Attribute { }
		class Test {

			[Ignore]
			public void TestWithoutIgnore() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithIgnore_WithReason_NoDiag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		[TestFixture]
		[Ignore(""ignore reason"")]
		class Test {

			[Test]
			public void TestWithIgnore() {
			}
		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithIgnore_WithoutReason_Case1_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		[TestFixture]
		[Ignore]
		class Test {

			[Test]
			public void TestWithIgnore() {
			}
		}
	}";
			AssertSingleDiagnostic( test, 7, 4 );
		}

		[Test]
		public void DocumentWithIgnore_WithoutReason_Case2_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		[TestFixture]
		class Test {

			[Test]
			[Ignore]
			public void TestWithIgnore() {
			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 5 );
		}

		[Test]
		public void DocumentWithIgnore_WithoutReason_Case3_Diag() {
			const string test = @"
	using System;
	using NUnit.Framework;

	namespace test {
		[TestFixture]
		[Ignore]
		class Test {

			[Test]
			[Ignore]
			public void TestWithIgnore() {
			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 7, 4 );
			var diag2 = CreateDiagnosticResult( 11, 5 );
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
				Id = IgnoreAttributeAnalyzer.DiagnosticId,
				Message = IgnoreAttributeAnalyzer.MessageFormat,
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
