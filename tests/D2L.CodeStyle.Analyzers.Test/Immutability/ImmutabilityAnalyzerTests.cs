using D2L.CodeStyle.Analyzers.Immutability;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {

    internal sealed class ImmutabilityAnalyzerTests : DiagnosticVerifier {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new ImmutabilityAnalyzer();
        }

        [Test]
        public void EmptyDocument_NoDiag() {
            const string test = @"";

            VerifyCSharpDiagnostic( test );
        }

        [Test]
        public void DocumentWithImmutableClass_ClassIsNotImmutable_Diag() {
            const string test = @"
    using System;

    namespace test {
        [Immutable]
        class Test {

            public DateTime bad = DateTime.Now;
            public DateTime badToo { get; set; }

        }
    }";
            AssertSingleDiagnostic( test, 5, 9 );
        }

        private void AssertNoDiagnostic( string file ) {
            VerifyCSharpDiagnostic( file );
        }

        private void AssertSingleDiagnostic( string file, int line, int column ) {
            var expected = new DiagnosticResult {
                Id = ImmutabilityAnalyzer.DiagnosticId,
                Message = ImmutabilityAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation( "Test0.cs", line, column )
                }
            };

            VerifyCSharpDiagnostic( file, expected );
        }
    }
}
