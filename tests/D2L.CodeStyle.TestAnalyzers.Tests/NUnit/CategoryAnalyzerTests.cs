using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {
	[TestFixture]
	internal sealed class CategoryAnalyzerTests : DiagnosticVerifier {

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
		private static readonly int PREAMBLE_LINES = PREAMBLE.Split( '\n' ).Length;

		[Test]
		public void Categorized_MethodMatched_Fixture_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.Category( ""fixture category"" )]
	class TestClass {
		[NUnit.Framework.Test]
		[NUnit.Framework.Category( ""Unit"" )]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_FixtureMatched_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.Category( ""Integration"" )]
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_InheritedFixtureMatched_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.Category( ""Integration"" )]
	class BaseClass : {}

	class TestClass : BaseClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_FixtureMatchedByFixture_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.TestFixture( Category = ""UI"" )]
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_FixtureMatchedByFixtureCsv_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.TestFixture( Category = ""Cats,Unit"" )]
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_InhertierFixtureMatchedByFixture_Assmebly_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.TestFixture( Category = ""UI"" )]
	class BaseClass : {}

	class TestClass : BaseClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_AssmeblyMatched_NoDiagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	class TestClass {
		[NUnit.Framework.Test]
		public void TestMethod( int x ) {}
	}
}";
			AssertNoDiagnostic(
				otherFile: @"
[assembly: NUnit.Framework.Category( ""Unit"" )]
",
				file: test
			);
		}

		[Test]
		public void Categorized_Method_Fixture_Assmebly_NoMatch_Diagnostic() {
			const string test = PREAMBLE + @"
namespace TestNamespace {
	[NUnit.Framework.Category( ""fixture category"" )]
	class TestClass {
		[NUnit.Framework.Test]
		[NUnit.Framework.Category( ""method category"" )]
		public void TestMethod( int x ) {}
	}
}";
			AssertSingleDiagnostic(
				diag: Diagnostics.NUnitCategory,
				otherFile: @"
[assembly: NUnit.Framework.Category( ""assembly category"" )]
",
				file: test,
				line: PREAMBLE_LINES + 6,
				column: 15,
				BuildWrongCategoriesMessage( "assembly category", "fixture category", "method category" )
			);
		}

		[Test]
		public void ProhibitedAssemblyCategory_Diagnostic() {
			const string test = @"
[assembly: NUnit.Framework.Category( ""Isolated"" )]
";
			AssertSingleDiagnostic(
				diag: Diagnostics.NUnitCategory,
				otherFile: PREAMBLE,
				file: test,
				line: 2,
				column: 12,
				$"Assemblies cannot be categorized as any of [Isolated], but saw 'Isolated'."
			);
		}

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
            AssertNoDiagnostic(otherFile:PREAMBLE, file : test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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
            AssertNoDiagnostic(otherFile: PREAMBLE, file: test);
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

        private static string BuildWrongCategoriesMessage(
			params string[] categories
		) =>
			$"Test must be categorized as one of [Integration, Load, System, UI, Unit], but saw [{string.Join( ", ", categories )}]. See http://docs.dev.d2l/index.php/Test_Categories.";


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
			return new CategoryAnalyzer();
		}
	}
}
