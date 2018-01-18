using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	internal sealed class ImmutabilityInheritanceAnalyzerTests : DiagnosticVerifier {

		private const string s_preamble = @"
using System;
using static D2L.CodeStyle.Annotations.Objects;

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public class Immutable : Attribute {
			public Except Except { get; set; }
		}

		[Flags]
		public enum Except {

			None = 0,
			ItHasntBeenLookedAt = 1,
			ItsSketchy = 2,
			ItsStickyDataOhNooo = 4,
			WeNeedToMakeTheAnalyzerConsiderThisSafe = 8,
			ItsUgly = 16,
			ItsOnDeathRow = 32

		}
	}
}
";

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ImmutabilityInheritanceAnalyzer();
		}

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void EmptyClass_ClassIsNotImmutable_NoDiag() {
			const string test = @"
namespace test {
	class Foo { }
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void EmptyClass_ClassIsImmutable_NoDiag() {
			const string test = @"
namespace test {
	[Immutable]
	class Foo { }
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithKnownImmutableField_ClassIsImmutable_NoDiag() {
			const string test = @"
namespace test {
	[Immutable]
	class Foo {
		public readonly int Foo = 0;
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void EmptyClass_WithImmutableExceptions_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo { }
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithKnownImmutableField_WithImmutableExceptions_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo {
		public readonly int Foo = 0;
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithoutSpecifiedExceptions_NoDiag() {
			const string test = @"
namespace test {
	[Immutable]
	class Bar { }

	[Immutable]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithMatchingSpecifiedExceptions_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithSubsetSpecifiedExceptions_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithProperSupersetSpecifiedExceptions_ErrorDiagnosticOnField() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly )]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 32, column: 23 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This member's type is marked immutable, but it has more permissive immutability than the current type. Shrink the exempted reason list on the member's type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithNoSpecifiedExceptions_ErrorDiagnosticOnField() {
			const string test = @"
namespace test {
	[Immutable]
	class Bar { }

	[Immutable( Except = Except.ItsUgly )]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 32, column: 23 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This member's type is marked immutable, but it has more permissive immutability than the current type. Shrink the exempted reason list on the member's type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void ClassWithMarkedImmutableField_WithSpecifiedExceptionsAsNone_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.None )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly )]
	class Foo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassImplementingImmutableInterface_InheritsInterfaceExceptions_ErrorDiagnosticOnField() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	interface IFoo { }
	
	[Immutable]
	class Bar { }

	class Foo : IFoo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 23 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This member's type is marked immutable, but it has more permissive immutability than the current type. Shrink the exempted reason list on the member's type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void ClassImplementingImmutableInterface_WithExplicitImmutabilityExceptionsSubset_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	interface IFoo { }
	
	[Immutable( Except = Except.None )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly )]
	class Foo : IFoo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassImplementingImmutableInterface_WithExplicitImmutabilityExceptionsProperSuperset_ErrorDiagnosticOnType() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	interface IFoo { }
	
	[Immutable( Except = Except.None )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo : IFoo {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 8 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This type is marked immutable, but it has more permissive immutability than its base type. Shrink the exempted reason list on this type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void ClassImplementingMultipleImmutableInterfaces_VerifiesAgainstAll_ErrorDiagnosticOnType() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	interface IFoo { }
	
	[Immutable( Except = Except.ItHasntBeenLookedAt )]
	interface IFoo2 { }

	[Immutable( Except = Except.ItsSketchy )]
	class Foo : IFoo, IFoo2 { }
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 8 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This type is marked immutable, but it has more permissive immutability than its base type. Shrink the exempted reason list on this type to be a subset of { ItsUgly }."
				},
				new DiagnosticResult {
					Id = Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 8 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This type is marked immutable, but it has more permissive immutability than its base type. Shrink the exempted reason list on this type to be a subset of { ItHasntBeenLookedAt }."
				}
			} );
		}

		[Test]
		public void ClassInheritingBaseType_InheritsBaseTypeExceptions_ErrorDiagnosticOnField() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	class FooBase { }
	
	[Immutable]
	class Bar { }

	class Foo : FooBase {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 23 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This member's type is marked immutable, but it has more permissive immutability than the current type. Shrink the exempted reason list on the member's type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void ClassInheritingBaseType_WithExplicitImmutabilityExceptionsSubset_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class FooBase { }
	
	[Immutable( Except = Except.None )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly )]
	class Foo : FooBase {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void ClassInheritingBaseType_WithExplicitImmutabilityExceptionsProperSuperset_ErrorDiagnosticOnType() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly )]
	class FooBase { }
	
	[Immutable( Except = Except.None )]
	class Bar { }

	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	class Foo : FooBase {
		public readonly Bar Foo = default( Bar );
	}
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 34, column: 8 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This type is marked immutable, but it has more permissive immutability than its base type. Shrink the exempted reason list on this type to be a subset of { ItsUgly }."
				}
			} );
		}

		[Test]
		public void InterfaceInheritingInterface_WithExplicitImmutabilityExceptionsSubset_NoDiag() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	interface IFoo { }

	[Immutable( Except = Except.ItsUgly )]
	interface IFoo2 : IFoo { }
}";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void InterfaceInheritingInterface_WithExplicitImmutabilityExceptionsProperSuperset_ErrorDiagnosticOnType() {
			const string test = @"
namespace test {
	[Immutable( Except = Except.ItsUgly  )]
	interface IFoo { }

	[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
	interface IFoo2 : IFoo { }
}";

			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType.Id,
					Locations = new[] { new DiagnosticResultLocation( "Test0.cs", line: 31, column: 12 ) },
					Severity = DiagnosticSeverity.Error,
					Message = "This type is marked immutable, but it has more permissive immutability than its base type. Shrink the exempted reason list on this type to be a subset of { ItsUgly }."
				}
			} );
		}

	}
}
