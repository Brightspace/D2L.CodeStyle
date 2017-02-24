using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.ParallelizableTests {
    [TestFixture]
    public class ParallelizableTestsAnalyzerTests : DiagnosticVerifier {

        private static readonly MetadataReference NUnitReference = MetadataReference.CreateFromFile( typeof( TestAttribute ).Assembly.Location );
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new ParallelizableTestsAnalyzer( new Dictionary<string, string> { { "System.DateTime", "System.DateTime" } }.ToImmutableDictionary() );
        }

        protected override MetadataReference[] GetAdditionalReferences() {
            return new[] { NUnitReference };
        }

        [Test]
        public void EmptyDocument_NoDiag() {
            const string test = @"";

            VerifyCSharpDiagnostic( test );
        }

        [Test]
        public void DocumentWithTest_UsesConstructorOnOffendingType_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public void Test() {
                var time = new DateTime();
            }

        }
    }";
            var expected = new DiagnosticResult {
                Id = ParallelizableTestsAnalyzer.DiagnosticId,
                Message = string.Format( ParallelizableTestsAnalyzer.MessageFormat, "System.DateTime", "System.DateTime" ),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 28)
                        }
            };

            VerifyCSharpDiagnostic( test, expected );
        }

        [Test]
        public void DocumentWithTest_UsesMethodOnOffendingType_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public void Test( DateTime d ) {
                var time = d.GetHashCode();
            }

        }
    }";
            var expected = new DiagnosticResult {
                Id = ParallelizableTestsAnalyzer.DiagnosticId,
                Message = string.Format( ParallelizableTestsAnalyzer.MessageFormat, "System.DateTime", "System.DateTime" ),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 28)
                        }
            };

            VerifyCSharpDiagnostic( test, expected );
        }

        [Test]
        public void DocumentWithTest_UsesMemberOnOffendingType_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public void Test() {
                var time = DateTime.Now;
            }

        }
    }";
            var expected = new DiagnosticResult {
                Id = ParallelizableTestsAnalyzer.DiagnosticId,
                Message = string.Format( ParallelizableTestsAnalyzer.MessageFormat, "System.DateTime", "System.DateTime" ),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 28)
                        }
            };

            VerifyCSharpDiagnostic( test, expected );
        }

    }
}