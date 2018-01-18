using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {
	[TestFixture]
	internal sealed class ValueSourceStringsAnalyzerTests : DiagnosticVerifier {

		private const string PREAMBLE = @"
using NUnit.Framework;
";

		[Test]
		public void TestMethod_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		[Test]
		public void Test() {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void TestMethod_ValueSource_NameOf_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		private static readonly IEnumerable<int> SOURCE = Enumerable.Empty<int>();

		[Test]
		public void Test( [ValueSource( nameof( SOURCE ) )] int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void TestMethod_ValueSource_SourceType_NameOf_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class SourceClass {
		private static readonly IEnumerable<int> CASES = Enumerable.Empty<int>();
	}

	class Test {
		[Test]
		public void Test( [ValueSource( typeof( SourceClass ), nameof( SourceClass.CASES )] int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		// This case should raise a diagnostic
		[Test]
		public void TestMethod_ValueSource_SourceType_StringName_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class SourceClass {
		public static readonly IEnumerable<int> CASES = Enumerable.Empty<int>();
	}

	class Test {
		[Test]
		public void Test( [ValueSource( typeof( SourceClass ), ""CASES"" )] int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void TestMethod_ValueSource_String_Diag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		private static readonly IEnumerable<int> SOURCE = Enumerable.Empty<int>();

		[Test]
		public void Test( [ValueSource( ""SOURCE"" )] ]int x ) {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.ValueSourceStrings, test, 9, 35, "SOURCE" );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( DiagnosticDescriptor diag, string file, int line, int column, params object[] messageArgs ) {
			DiagnosticResult result = new DiagnosticResult {
				Id = diag.Id,
				Message = string.Format( diag.MessageFormat.ToString(), messageArgs ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};

			VerifyCSharpDiagnostic( file, result );
		}


		private static readonly MetadataReference NUnitReference = MetadataReference.CreateFromFile( typeof( TestAttribute ).Assembly.Location );

		protected override MetadataReference[] GetAdditionalReferences() => new[] {
			NUnitReference
		};

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ValueSourceStringsAnalyzer();
		}
	}
}
