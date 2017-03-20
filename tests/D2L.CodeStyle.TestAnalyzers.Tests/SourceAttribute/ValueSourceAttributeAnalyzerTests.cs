using D2L.CodeStyle.TestAnalyzers.ParallelizableTests;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.SourceAttribute {

	internal sealed class ValueSourceAttributeAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ValueSourceAttributeAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutValueSource_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {

			[Test]
			public void TestWithoutValueSource() {
			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithValueSource_WithStatic_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private static readonly PluginTuple[] KnownPlugins = new[] { new PluginTuple() };

			[Test]
			public void test4( [ValueSource( nameof( KnownPlugins ) )] PluginTuple plugin ) {

			}

		}
	}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithValueSource_WithoutStatic_Case1_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private readonly PluginTuple[] KnownPlugins = new[] { new PluginTuple() };

			[Test]
			public void test4( [ValueSource( nameof( KnownPlugins ) )] PluginTuple plugin ) {

			}
		}
	}";
			AssertSingleDiagnostic( test, 9, 24, "KnownPlugins" );
		}

		[Test]
		public void DocumentWithValueSource_WithoutStatic_Case2_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private String ValidCases {
				return ""test"";
			}

			public void test1( [ValueSource( ""ValidCases"" )] String s ) {

			}
		}
	}";
			AssertSingleDiagnostic( test, 10, 24, "ValidCases" );
		}

		[Test]
		public void DocumentWithValueSource_WithoutStatic_Case3_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private Array GetContractVersions() {
				return Enum.GetValues( typeof( JsonContractVersion ) );
			}

			[Test]
			public void test4( [ValueSource( ""GetContractVersions"" )] JsonContractVersion contractVersion ) {

			}
		}
	}";
			AssertSingleDiagnostic( test, 11, 24, "GetContractVersions" );
		}

		[Test]
		public void DocumentWithValueSource_WithoutStatic_Case4_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			static Array GetContractVersions() {
				return Enum.GetValues( typeof( JsonContractVersion ) );
			}

			[Test]
			public void test4( [ValueSource( typeof(Foo), ""GetContractVersions"" )] JsonContractVersion contractVersion ) {

			}
		}
		public class Foo {
			public Array GetContractVersions() {
				return Enum.GetValues( typeof( JsonContractVersion ) );
			}
		}

		public class MyFoo {
			public Array GetContractVersions() {
				return Enum.GetValues( typeof( JsonContractVersion ) );
			}
		}
	}";
			AssertSingleDiagnostic( test, 11, 24, "GetContractVersions" );
		}

		[Test]
		public void DocumentWithValueSource_WithoutStatic_Case5_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Test {
			private Array GetContractVersions() {
				return Enum.GetValues( typeof( JsonContractVersion ) );
			}

			private Array GetHealthStatusCodes() {
				return Enum.GetValues( typeof( HealthStatusCode ) );
			}

			[Test]
			public void test4( [ValueSource( ""GetContractVersions"" )] JsonContractVersion contractVersion, [ValueSource( ""GetHealthStatusCodes"" )] HealthStatusCode healthStatusCode ) {

			}
		}
	}";
			var diag1 = CreateDiagnosticResult( 15, 24, "GetContractVersions" );
			var diag2 = CreateDiagnosticResult( 15, 100, "GetHealthStatusCodes" );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
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
				Id = ValueSourceAttributeAnalyzer.DiagnosticId,
				Message = string.Format( ValueSourceAttributeAnalyzer.MessageFormat, message ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
