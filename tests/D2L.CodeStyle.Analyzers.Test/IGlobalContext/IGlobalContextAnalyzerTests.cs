using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.IGlobalContext {
	internal sealed class IGlobalContextAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new IGlobalContextAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithoutIGlobalContext_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public DateTime good = DateTime.Now;
            public DateTime goodToo { get; set; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithIGlobalContext_Field_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {
			private IGlobalContext m_global;

        }
    }";
			AssertSingleDiagnostic( test, 6, 12 );
		}

		[Test]
		public void DocumentWithIGlobalContext_Property_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {
			public IGlobalContext Global {
				get { return m_global; }
			}
        }
    }";
			AssertSingleDiagnostic( test, 6, 11 );
		}

		[Test]
		public void DocumentWithIGlobalContext_MethodReturnType_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {
			private IGlobalContext m_global;
			IGlobalContext GetGlobalContext() { 
				return m_global;
			}
        }
    }";
			var diag1 = CreateDiagnosticResult( 6, 12 );
			var diag2 = CreateDiagnosticResult( 7, 4 );
			VerifyCSharpDiagnostic( test, diag1, diag2 );
		}

		[Test]
		public void DocumentWithIGlobalContext_MethodParameter_Diag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {
			void GetGlobalContext(IGlobalContext globalContext) { 
			}
        }
    }";
			AssertSingleDiagnostic( test, 6, 26 );
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
				Id = IGlobalContextAnalyzer.DiagnosticId,
				Message = IGlobalContextAnalyzer.MessageFormat,
				Severity = DiagnosticSeverity.Info,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
