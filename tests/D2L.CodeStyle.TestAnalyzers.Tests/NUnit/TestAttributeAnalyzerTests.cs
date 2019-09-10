using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.NUnit;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;


namespace D2L.CodeStyle.TestAnalyzers.NUnit {
    [TestFixture]
    internal sealed class TestAttributeAnalyzerTests : DiagnosticVerifier {

        private const string PREAMBLE = @"
namespace NUnit.Framework {
	public abstract class NUnitAttribute : System.Attribute {}

	public class CategoryAttribute : NUnitAttribute { public CategoryAttribute( string name ) {} }
	public class TestFixtureAttribute : NUnitAttribute {
		public string Category { get; set; }
	}

	public class TestAttribute : NUnitAttribute {}
	public class TestCaseAttribute : NUnitAttribute {}
	public class TestCaseSourceAttribute : NUnitAttribute {}
}
";

        [Test]
        public void TestAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void TestAttribute_NoFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void NoTestAttribute_NoFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void TheoryAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.Theory]
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void TestCaseAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.TestCase(1)]
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void Mulitple_TestCaseAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.TestCase(1)]
		[NUnit.Framework.TestCase(2)]
		[NUnit.Framework.TestCase(3)]
		public void TestMethod( int x ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void TestCaseSourceAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.TestCaseSource(""cases"")]
		public void TestMethod( int x ) {}
	}
    static object[] cases = {
        new object[] { 1 },
        new object[] { 2 },
        new object[] { 3 }
    }; 
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void SetUpAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.SetUp]
		public void TestMethod(  ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void OTSetUpAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.OneTimeSetUp]
		public void TestMethod(  ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void TearDownAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.TearDown]
		public void TestMethod(  ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void OTTearDownAttribute_InFixture_NoDiagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TextFixture]
    [NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		[NUnit.Framework.OneTimeTearDown]
		public void TestMethod(  ) {}
	}
}";
            AssertNoDiagnostic( otherFile: PREAMBLE, file: test );
        }

        [Test]
        public void NoTestAttribute_InFixture_Diagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
		public void TestMethod( int x ) {}
	}
}";
            AssertSingleDiagnostic(
                diag: Diagnostics.TestAttributeMissed,
                otherFile: PREAMBLE,
                file: test,
                line: 6,
                column: 15,
                $"TestMethod"
            );
        }

        [Test]
        public void NoTest_HasAttribute_InFixture_Diagnostic() {
            const string test = @"
namespace TestNamespace {
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.Category( ""Unit"" )]
	class TestClass {
        [NUnit.Framework.Explicit]
		public void TestMethod( int x ) {}
	}
}";
            AssertSingleDiagnostic(
                diag: Diagnostics.TestAttributeMissed,
                otherFile: PREAMBLE,
                file: test,
                line: 7,
                column: 15,
                $"TestMethod"
            );
        }

        private void AssertNoDiagnostic( string file, string otherFile ) {
            VerifyCSharpDiagnostic( sources: new[] { file, otherFile } );
        }

        private void AssertSingleDiagnostic(
            DiagnosticDescriptor diag,
            string otherFile,
            string file,
            int line,
            int column,
            params object[] messageArgs
        ) {
            DiagnosticResult result = new DiagnosticResult {
                Id = diag.Id,
                Message = string.Format( diag.MessageFormat.ToString(), messageArgs ),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation( "Test1.cs", line, column )
                }
            };

            VerifyCSharpDiagnostic(
                sources: new[] {
                    otherFile,
                    file,
                }, result
            );
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new TestAttributeAnalyzer();
        }
    }
}
