using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Immutability {

	internal sealed class UnsafeStaticsAnalyzerTests : DiagnosticVerifier {
        private static readonly MutabilityInspectionResultFormatter s_formatter = new MutabilityInspectionResultFormatter();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new UnsafeStaticsAnalyzer();
		}

		private const string s_preamble = @"
using D2L.CodeStyle.Annotations;
namespace D2L.CodeStyle.Annotations {
	public class Statics {
		public class Audited : Attribute {}
		public class Unaudited : Attribute {}
	}
	public class Objects {
		public class Immutable : Attribute {}
	}
}
";

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test] // X
		public void DocumentWithoutStatic_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public DateTime good = DateTime.Now;
            public DateTime goodToo { get; set; }

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
		}

		[Test] // X
        public void DocumentWithStatic_ReadonlySelfReferencingStatic_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public sealed class Foo {
                public static readonly Foo Default = new Foo();
            }
            public static readonly Foo good = new Foo();

        }
    }";
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void DocumentWithStatic_ReadonlySelfReferencingStaticOfMutableType_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            public sealed class Foo {
                private int uhoh = 1;
                public static readonly Foo Default = new Foo();
            }
            public static readonly Foo good = new Foo();

        }
    }";
            var diag1 = CreateDiagnosticResult( 20, 44, "Default", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "Default.uhoh",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
            var diag2 = CreateDiagnosticResult( 22, 40, "good", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "good.uhoh",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
            VerifyCSharpDiagnostic( s_preamble + test, diag1, diag2 );
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
			AssertSingleDiagnostic( s_preamble + test, 22, 31, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
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

            [Statics.Unaudited]
            public static Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
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

            [Statics.Audited]
            public static Foo bad = new Foo();

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
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
			AssertSingleDiagnostic( s_preamble + test, 22, 40, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad.ClientsName",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
		}

		[Test] // X
		public void DocumentWithStaticField_ReadonlyValueType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            public static readonly DateTime good = DateTime.Now;

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
		}

        [Test]
        public void DocumentWithStaticField_ReadonlyNotSealedImmutableUnknownConcreteType_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo {
                public readonly string ClientsName = ""YOLO"";
            }

            public static readonly Foo bad = GetFoo();

            private static Foo GetFoo() {
                return new Foo();
            }

        }
    }";

			// Although a concrete instance of Foo is safe, we don't look
			// inside GetFoo to see that its returning a concrete Foo and
			// not some derived class.
			AssertSingleDiagnostic( s_preamble + test, 22, 40, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Type,
                cause: MutabilityCause.IsNotSealed
            ) );
		}

        [Test] // X
        public void DocumentWithStaticField_ReadonlyNotSealedImmutableKnownConcreteType_NoDiag() {
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

            AssertNoDiagnostic( s_preamble + test );
        }

		[Test] // X
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
			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStaticField_ImmutableFieldWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Objects.Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static readonly Foo good;

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
		}

        [Test] // X
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
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void DocumentWithStaticField_InterfaceWithMutableConcreteInitializer_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public string ClientsName = ""YOLO"";
            }

            public readonly static IFoo bad = new Foo();

        }
    }";
            AssertSingleDiagnostic( s_preamble + test, 23, 41, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad.ClientsName",
                membersTypeName: "System.String",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
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
            AssertSingleDiagnostic( s_preamble + test, 17, 61, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "System.Collections.IList",
                kind: MutabilityTarget.Type,
                cause: MutabilityCause.IsAnInterface
            ) );
        }

        [Test]
        public void DocumentWithStaticCollectionField_UnsealedContainer_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {
            public static readonly System.Collections.Generic.List<object> bad;

        }
    }";
            AssertSingleDiagnostic( s_preamble + test, 17, 76, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "System.Collections.Generic.List",
                kind: MutabilityTarget.Type,
                cause: MutabilityCause.IsNotSealed
            ) );
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
            AssertSingleDiagnostic( s_preamble + test, 17, 87, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "System.Object",
                kind: MutabilityTarget.TypeArgument,
                cause: MutabilityCause.IsNotSealed
            ) );
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
            AssertNoDiagnostic( s_preamble + test );
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
            AssertNoDiagnostic( s_preamble + test );
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
			AssertSingleDiagnostic( s_preamble + test, 22, 13, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
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

            [Statics.Unaudited]
            public static Foo bad { get; set; }

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
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

            [Statics.Audited]
            public static Foo bad { get; set; }

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
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

            public static Foo bad { get; } = new Foo()

        }
    }";
			AssertSingleDiagnostic( s_preamble + test, 22, 13, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad.ClientsName",
                membersTypeName: "System.String",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
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
			AssertNoDiagnostic( s_preamble + test );
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
			AssertNoDiagnostic( s_preamble + test );
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
			AssertSingleDiagnostic( s_preamble + test, 22, 13, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
		}

		[Test]
        public void DocumentWithStaticProperty_ImplementedGetter_NoDiag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            internal class Foo { 
                private string m_mutable = null;
            }

            // safe, because it's not a static variable at all
            public static Foo good { 
                get { 
                    return new Foo(); 
                } 
            }

        }
    }";
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
		public void DocumentWithStaticProperty_ImmutablePropertyWithImmutableMarkedType_NoDiag() {
			const string test = @"
    using System;

    namespace test {
        class Tests {

            [Objects.Immutable] // yes, this isn't actually immutable, that's the point
            internal class Foo {
                public string ClientsName = ""YOLO"";
            }

            public static Foo good { get; }

        }
    }";
			AssertNoDiagnostic( s_preamble + test );
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
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void DocumentWithStaticProperty_InterfaceWithMutableConcreteInitializer_Diag() {
            const string test = @"
    using System;

    namespace test {
        class Tests {

            interface IFoo {}
            internal sealed class Foo : IFoo {
                public string ClientsName = ""YOLO"";
            }

            public static IFoo bad { get; } = new Foo();

        }
    }";
            AssertSingleDiagnostic( s_preamble + test, 23, 13, "bad", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "bad.ClientsName",
                membersTypeName: "System.String",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
        }

		[Test]
		public void DocumentWithOneLevelRecurrsiveTypes_Immutable_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Foo Instance;
			}
		}
	}";

			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithOneLevelRecurrsiveTypes_Mutable_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal class Foo {
				public Foo Instance;
			}
		}
	}";

			AssertSingleDiagnostic( s_preamble + test, 18, 41, "foo", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "foo.Instance",
                membersTypeName: "test.Tests.Foo",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
		}

		[Test]
		public void DocumentWithMultiLevelRecurrsiveTypes_Immutable_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

            private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Bar Bar = null;
			}

			internal sealed class Bar {
				public readonly Foo Foo = new Foo();
			}
		}
	}";

			AssertNoDiagnostic( s_preamble + test );
		}


		[Test]
		public void DocumentWithMultiLevelRecurrsiveTypes_Mutable_Diag() {
			const string test = @"
	using System;

	namespace test {
		class Tests {

			private readonly static Foo foo = new Foo();

			internal sealed class Foo {
				public readonly Bar Bar; // Bar is not sealed, so this is not immutable
			}

			internal sealed class Bar {
				public readonly Foo Foo;
			}
		}
	}";

			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithNonConstructorInitializerOfUnsealedType_Diag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			interface IUnsafe { void Magic(); } // could be anythinggggggg

			class Safe : IUnsafe {
				void IUnsafe.Magic() {} // looks safe to me
				public static readonly Safe Instance { get; } = new Safe();
			}

			private readonly static IUnsafe foo = Safe.Instance; // bad, Safe is not sealed
		}
	}";

			AssertSingleDiagnostic( s_preamble + test, 23, 36, "foo", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "foo",
                membersTypeName: "test.Tests.Safe",
                kind: MutabilityTarget.Type,
                cause: MutabilityCause.IsNotSealed
            ) );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithNonConstructorInitializerOfSealedType_NoDiag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			interface IUnsafe { void Magic(); } // could be anythinggggggg

			sealed class Safe : IUnsafe {
				void IUnsafe.Magic() {} // looks safe to me
				public static readonly Safe Instance { get; } = new Safe();
			}

			private readonly static IUnsafe foo = Safe.Instance;
		}
	}";

			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStaticField_ReadonlyUnsafeBaseClassWithSafeInitializer_NoDiag2() {
			const string test = @"
	using System;
	using System.Collections.Generic;
	namespace test {
		class Tests {
			private readonly static IEqualityComparer<string> foo = StringComparer.Ordinal;
		}
	}";

			AssertNoDiagnostic( s_preamble + test );
		}

		[Ignore( "This is an unlikely-to-be-used hole in the analyzer that we need to fix regardless" )]
		public void DocumentWithStaticField_TypeIsUnsafeInitializerIsImplicitConversionFromSafeValue_Diag() {
			const string test = @"
	using System;
	namespace test {
		class Tests {
			sealed class Foo {
				public Foo(int xx) { x = xx; }

				public static implicit operator Foo(int x) {
					return new Foo(x);
				}

				public int x; // makes Foo mutable
			}

			private static readonly Foo foo = 3;
		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 26, 32, "foo", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "foo.x",
                membersTypeName: "System.Int32",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
		}

		[Test]
		public void DocumentWithAuditedSafeThing_Diag() {
			const string test = @"
namespace test {
	class tests {
		[Statics.Audited("""", """", """")]
		private readonly static string x = ""hey""
	}
}";
			var expected = new DiagnosticResult {
				Id = Diagnostics.UnnecessaryStaticAnnotation.Id,
				Message = string.Format( Diagnostics.UnnecessaryStaticAnnotation.MessageFormat.ToString(), "Statics.Audited", "x" ),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", 16, 34),
				}
			};

			VerifyCSharpDiagnostic( s_preamble + test, expected );
		}

		[Test]
		public void DocumentWithUnauditedSafeThing_Diag() {
			const string test = @"
namespace test {
	class tests {
		[Statics.Unaudited( Because.ItsSketchy )]
		private readonly static string x = ""hey""
	}
}";
			var expected = new DiagnosticResult {
				Id = Diagnostics.UnnecessaryStaticAnnotation.Id,
				Message = string.Format( Diagnostics.UnnecessaryStaticAnnotation.MessageFormat.ToString(), "Statics.Unaudited", "x" ),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", 16, 34),
				}
			};

			VerifyCSharpDiagnostic( s_preamble + test, expected );
		}

		[Test]
		public void DocumentWithConflictingAnnotations_Diag() {
			const string test = @"
namespace test {
	class tests {
		[Statics.Unaudited( Because.ItsSketchy )]
		[Statics.Audited("""", """", """")]
		private static string x = ""hey""
	}
}";
			var expected = new DiagnosticResult {
				Id = Diagnostics.ConflictingStaticAnnotation.Id,
				Message = Diagnostics.ConflictingStaticAnnotation.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", 17, 25),
				}
			};

			VerifyCSharpDiagnostic( s_preamble + test, expected );
		}

        [Test]
        public void ReadOnlyProperty_NoDiagnostic() {
            const string test = @"
 namespace test {
    class tests {
        public static int ReadOnlyProperty { get; }
    }
}";
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void ReadOnlyPropertyWithInitializer_NoDiagnostic() {
            const string test = @"
 namespace test {
    class tests {
        public static object readonlyproperty { get; } = new string();
    }
}";
            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void NonReadOnlyProperty_Diagnostic() {
            const string test = @"
 namespace test {
    class tests {
        public static int PropertyWithSetter { get; set; }
    }
}";
			AssertSingleDiagnostic( s_preamble + test, 15, 9, "PropertyWithSetter", MutabilityInspectionResult.Mutable(
                mutableMemberPath: "PropertyWithSetter",
                membersTypeName: "Widget",
                kind: MutabilityTarget.Member,
                cause: MutabilityCause.IsNotReadonly
            ) );
        }

        [Test]
        public void PropertyWithImplementedGetterOnly_NoDiagnostic() {
            const string test = @"
 namespace test {
    class tests {
        [Statics.Unaudited("""", """", """")]
        public static int[] m_things;

        public static int[] Things {
            get { m_things = value; }
        }
    }
}";
            AssertNoDiagnostic( s_preamble + test );
        }

		[Test]
		public void NonAutoPropertyWithInitializer_NoDiagnostic() {
			const string test = @"
 namespace test {
    class tests {
        public static int[] shittyarray => new int[] {1,2,3};
    }
}";
			AssertNoDiagnostic( s_preamble + test );
		}

        [Test]
        public void PropertyWithImplementedSetterOnly_NoDiagnostic() {
            const string test = @"
 namespace test {
    sealed class tests {
        [Statics.Unaudited("""", """", """")]
        public static int[] m_things;

        public static int[] Things {
            set { m_things = value; }
        }
    }
}";

            AssertNoDiagnostic( s_preamble + test );
        }

        [Test]
        public void PropertyWithImplementedGetterSetter_NoDiagnostic() {
            const string test = @"
 namespace test {
    sealed class tests {
        [Statics.Unaudited("""", """", """")]
        public static int[] m_things;

        public static int[] Things {
            get { return m_things; }
            set { m_things = value; }
        }
    }
}";

            AssertNoDiagnostic( s_preamble + test );
        }


		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column, string fieldOrProp, MutabilityInspectionResult inspectionResult ) {

            DiagnosticResult result = CreateDiagnosticResult( line, column, fieldOrProp, inspectionResult );
            VerifyCSharpDiagnostic( file, result );
        }

        private static DiagnosticResult CreateDiagnosticResult( int line, int column, string fieldOrProp, MutabilityInspectionResult result ) {
            var reason = s_formatter.Format( result );

            return new DiagnosticResult {
                Id = Diagnostics.UnsafeStatic.Id,
                Message = string.Format( Diagnostics.UnsafeStatic.MessageFormat.ToString(), fieldOrProp, reason ),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation( "Test0.cs", line, column )
                }
            };
        }
    }
}
