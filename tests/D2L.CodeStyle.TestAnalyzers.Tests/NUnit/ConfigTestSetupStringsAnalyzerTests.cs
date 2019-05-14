using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {
	[TestFixture]
	internal sealed class ConfigTestSetupStringsAnalyzerTests : DiagnosticVerifier {

		private const string PREAMBLE = @"
using System;
using D2L.LP.Configuration.Config;
using NUnit.Framework;

namespace D2L.LP.Configuration.Config {
	[AttributeUsage( AttributeTargets.Method | AttributeTargets.Class )]
	public sealed class ConfigTestSetupAttribute : Attribute {
		public ConfigTestSetupAttribute( string sourceName ) { }
	}
}
";

		[Test]
		public void NoAttribute_NoDiag() {
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
		public void MethodAttribute_WithNameOf_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		private static readonly object SourceName = new object();

		[Test]
		[ConfigTestSetup( nameof( SourceName ) )]
		public void Test() {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void MethodAttribute_WithLiteral_Diag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		private static readonly object SourceName = new object();

		[Test]
		[ConfigTestSetup( ""SourceName"" )]
		public void Test() {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.ConfigTestSetupStrings, test, 18, 21, "SourceName" );
		}

		[Test]
		public void MethodAttribute_WithConstant_Diag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		private const string Source = ""SourceName"";
		private static readonly object SourceName = new object();

		[Test]
		[ConfigTestSetup( Source )]
		public void Test() {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.ConfigTestSetupStrings, test, 19, 21, "..." );
		}

		[Test]
		public void FixtureAttribute_WithNameOf_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	[ConfigTestSetup( nameof( SourceName ) )]
	class Test {
		private static readonly object SourceName = new object();

		[Test]
		public void Test() {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void FixtureAttribute_WithLiteral_Diag() {
			const string test = PREAMBLE + @"
namespace Test {
	[ConfigTestSetup( ""SourceName"" )]
	class Test {
		private static readonly object SourceName = new object();

		[Test]
		public void Test() {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.ConfigTestSetupStrings, test, 14, 20, "SourceName" );
		}

		[Test]
		public void FixtureAttribute_WithConstant_Diag() {
			const string test = PREAMBLE + @"
namespace Test {
	[ConfigTestSetup( Source )]
	class Test {
		public const string Source = ""SourceName"";
		private static readonly object SourceName = new object();

		[Test]
		public void Test() {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.ConfigTestSetupStrings, test, 14, 20, "..." );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic(
				DiagnosticDescriptor diag,
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
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};

			VerifyCSharpDiagnostic( file, result );
		}


		private static readonly MetadataReference NUnitReference =
			MetadataReference.CreateFromFile( typeof(TestAttribute).Assembly.Location );

		protected override MetadataReference[] GetAdditionalReferences() => new[] {
			NUnitReference
		};

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ConfigTestSetupStringsAnalyzer();
		}

	}
}
