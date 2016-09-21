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

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad = new Foo();

        }
    }";
			AssertSingleDiagnostic( test, 11, 31, "bad", "it" );
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
			AssertSingleDiagnostic( test, 11, 40, "bad", "test.Tests.Foo" );
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
		public void DocumentWithStaticField_ReadonlyNotSealedImmutable_NoDiag() {
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

			AssertSingleDiagnostic( test, 11, 40, "bad", "test.Tests.Foo" );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlySealedImmutable_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void DocumentWithStaticField_ImmutableFieldWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static readonly Foo good;

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
        public void DocumentWithStaticField_InterfaceWithImmutableConcreteInitializer_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public readonly string ClientsName = ""YOLO"";
            }

            public readonly static IFoo good = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
        public void DocumentWithStaticCollectionField_NonGeneric_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.IList bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 61, "bad", "System.Collections.IList" );
        }

        [Test]
        public void DocumentWithStaticCollectionField_GenericObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Generic.List<object> bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 76, "bad", "System.Collections.Generic.List<System.Object>" );
        }

        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Immutable.ImmutableList<object> bad;

        }
    }";
            AssertSingleDiagnostic( test, 6, 87, "bad", "System.Collections.Immutable.ImmutableList<System.Object>" );
        }

        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericImmutableObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Immutable.ImmutableList<int> good;

        }
    }";
            AssertNoDiagnostic( test );
        }


        [Test]
        public void DocumentWithStaticImmutableCollectionField_GenericImmutableMarkedObject_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            [Objects.Immutable]
            class Foo {
                void MethodsMakesMeNotDeterministicallyImmutable() {}
            }
            public static readonly System.Collections.Immutable.ImmutableList<Foo> good;

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

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; set; }

        }
    }";
			AssertSingleDiagnostic( test, 11, 13, "bad", "it" );
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
			AssertSingleDiagnostic( test, 11, 13, "bad", "test.Tests.Foo" );
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

            internal sealed class Foo {
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

            internal sealed class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static Foo bad { get; private set; }

        }
    }";
			AssertSingleDiagnostic( test, 11, 13, "bad", "it" );
		}

		[Test]
		public void DocumentWithStaticProperty_ImmutablePropertyWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static Foo good { get; }

        }
    }";
			AssertNoDiagnostic( test );
		}

		[Test]
        public void DocumentWithStaticProperty_InterfaceWithImmutableConcreteInitializer_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static IFoo good { get; } = new Foo();

        }
    }";
            AssertNoDiagnostic( test );
        }

        [Test]
		public void DocumentWithRecurrsiveTypes() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

			internal static class Foo {
				public static readonly Bar Bar = null;
			}

			internal static class Bar {
				public static readonly Foo Foo = null;
			}
		}
	}";

			DiagnosticResult result1 = CreateDiagnosticResult( 8, 32, "Bar", "test.Tests.Bar" );
			DiagnosticResult result2 = CreateDiagnosticResult( 12, 32, "Foo", "test.Tests.Foo" );
			VerifyCSharpDiagnostic( test, result1, result2 );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column, string fieldOrProp, string badFieldOrType ) {

			DiagnosticResult result = CreateDiagnosticResult( line, column, fieldOrProp, badFieldOrType );
			VerifyCSharpDiagnostic( file, result );
		}

		private static DiagnosticResult CreateDiagnosticResult( int line, int column, string fieldOrProp, string badFieldOrType ) {
			return new DiagnosticResult {
				Id = UnsafeStaticsAnalyzer.DiagnosticId,
				Message = string.Format( UnsafeStaticsAnalyzer.MessageFormat, fieldOrProp, badFieldOrType ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
