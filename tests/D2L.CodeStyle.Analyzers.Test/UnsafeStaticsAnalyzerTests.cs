using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2L.CodeStyle.Analysis;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {

    internal sealed class UnsafeStaticsAnalyzerTests : DiagnosticVerifier {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new UnsafeStaticsAnalyzer();
        }

        [Test]
        public void EmptyDocument_NoDiag() {
            const string test = @"";

            VerifyCSharpDiagnostic( test );
        }

        [Test]
        public void DocumentWithoutStatic_NoDiag() {
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
        public void DocumentWithStaticField_NonReadonly_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad = new Foo();

        }
    }";
            AssertSingleDiagnostic( test, 11, 31, BadStaticReason.StaticIsMutable );
        }

        [Test]
        public void DocumentWithStaticField_NonReadonlyUnaudited_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Unaudited]
            public static Foo bad = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }


        [Test]
        public void DocumentWithStaticField_NonReadonlyAudited_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Audited]
            public static Foo bad = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticField_ReadonlyButMutable_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";
            AssertSingleDiagnostic( test, 11, 40, BadStaticReason.TypeOfStaticIsMutable );
        }

        [Test]
        public void DocumentWithStaticField_ReadonlyValueType_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public static readonly DateTime good = DateTime.Now;

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticField_ReadonlyImmutable_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticProperty_NonReadonly_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; set; }

        }
    }";
            AssertSingleDiagnostic( test, 11, 13, BadStaticReason.StaticIsMutable );
        }


        [Test]
        public void DocumentWithStaticProperty_NonReadonlyUnaudited_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Unaudited]
            public static Foo bad { get; set; }

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticProperty_NonReadonlyAudited_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            [CodeStyle.Statics.Audited]
            public static Foo bad { get; set; }

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticProperty_ReadonlyButMutable_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; }

        }
    }";
            AssertSingleDiagnostic( test, 11, 13, BadStaticReason.TypeOfStaticIsMutable );
        }

        [Test]
        public void DocumentWithStaticProperty_ReadonlyValueType_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public static DateTime good { get; }

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticProperty_ReadonlyImmutable_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; }

        }
    }";
            AssertNoDiagnostic( test );
        }


        [Test]
        public void DocumentWithStaticProperty_PrivateSetterImmutable_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; private set; }

        }
    }";
            AssertSingleDiagnostic( test, 11, 13, BadStaticReason.StaticIsMutable );
        }

        private void AssertNoDiagnostic( string file ) {
            VerifyCSharpDiagnostic( file );
        }

        private void AssertSingleDiagnostic( string file, int line, int column, BadStaticReason reason ) {
            var expected = new DiagnosticResult {
                Id = UnsafeStaticsAnalyzer.DiagnosticId,
                Message = string.Format( UnsafeStaticsAnalyzer.MessageFormat, reason ),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation( "Test0.cs", line, column )
                }
            };

            VerifyCSharpDiagnostic( file, expected );
        }
    }
}
