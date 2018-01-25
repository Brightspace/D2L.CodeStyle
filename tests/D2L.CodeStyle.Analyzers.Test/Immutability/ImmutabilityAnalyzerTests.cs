using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Immutability {

	internal sealed class ImmutabilityAnalyzerTests : DiagnosticVerifier {
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new ImmutabilityAnalyzer();
		}

		private const string s_preamble = @"
using System;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;
 
namespace D2L.CodeStyle.Annotations {
	public class Objects {
		public class Immutable : Attribute {}
		public sealed class Immutable : Attribute {
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
 
	public class Mutability {
		public class UnauditedAttribute : Attribute {
			public UnauditedAttribute( Because cuz ) { }
		}
	}
 
	public enum Because {
		ItHasntBeenLookedAt = 1,
		ItsSketchy = 2,
		ItsStickyDataOhNooo = 3,
		WeNeedToMakeTheAnalyzerConsiderThisSafe = 4,
		ItsUgly = 5,
		ItsOnDeathRow = 6
	}
}
";

		private readonly MutabilityInspectionResultFormatter m_formatter = new MutabilityInspectionResultFormatter();

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButIsMarkedImmutable_Diag() {
			const string test = @"
	namespace test {
		[Immutable]
		class Test {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 43, 9, MutabilityInspectionResult.Mutable(
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}


		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButImplementsImmutableInterface_Diag() {
			const string test = @"
	namespace test {
		[Immutable] interface IFoo {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 43, 9, MutabilityInspectionResult.Mutable(
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}

		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButImplementsImmutableInterfaceInChain_Diag() {
			const string test = @"
	namespace test {
		[Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 44, 9, MutabilityInspectionResult.Mutable( 
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}

		[Test]
		public void DocumentWithStatic_ClassIsImmutableAndImplementsImmutableInterface_NoDiag() {
			const string test = @"
	namespace test {
		[Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public readonly DateTime bad = DateTime.Now;
			public DateTime badToo { get; }

		}
	}";
			AssertNoDiagnostic( s_preamble + test );
		}

		#region Unaudited Reason Verification Diagnostic

		[Test]
		public void DocumentWithStatic_ClassIsImmutableWithNoExceptionsAndHasUnauditedField_NoDiag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable]
		class Test {
 
			[Mutability.Unaudited( Because.ItsUgly )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassIsImmutableWithExceptionsAndHasUnauditedFieldThatIsExcepted_NoDiag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItsUgly )]
		class Test {
 
			[Mutability.Unaudited( Because.ItsUgly )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			AssertNoDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassIsImmutableWithExceptionsAndHasUnauditedFieldThatIsNotExcepted_Diag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItsUgly )]
		class Test {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItHasntBeenLookedAt }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 45, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsImmutableInterfaceWithExceptionsAndHasUnauditedFieldThatIsNotExcepted_Diag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItsUgly )]
		interface ITest { }
 
		class Test : ITest {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItHasntBeenLookedAt }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 47, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsMultipleImmutableInterfaceWithExceptionsAndHasUnauditedFieldThatIsNotExcepted_Diag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItsUgly )]
		interface ITest { }
 
		[Immutable( Except = Except.ItsSketchy | Except.ItsUgly )]
		interface ITest2 { }
 
		class Test : ITest, ITest2 {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItHasntBeenLookedAt }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 50, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsMultipleImmutableInterfaceWithExceptionsAndHasUnauditedFieldThatIsOnlyExceptedByOne_Diag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		interface ITest { }
 
		[Immutable( Except = Except.None )]
		interface ITest2 { }
 
		class Test : ITest, ITest2 {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItHasntBeenLookedAt }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 50, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsMultipleImmutableInterfaceWithExceptionsAndHasUnauditedFieldThatIsExceptedByBoth_NoDiag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		interface ITest { }
 
		[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
		interface ITest2 { }
 
		class Test : ITest, ITest2 {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsImmutableInterfaceWithExceptionsAndHasItsOwnImmutableExceptionsAndHasUnauditedFieldThatIsNotExcepted_Diag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt | Except.ItsUgly )]
		interface ITest { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		class Test : ITest {
 
			[Mutability.Unaudited( Because.ItsSketchy )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItsSketchy }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 48, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		[Test]
		public void DocumentWithStatic_ClassImplementsImmutableInterfaceWithExceptionsAndHasItsOwnImmutableExceptionsAndHasUnauditedFieldThatIsExceptedByBoth_NoDiag() {
			const string test = @"
	namespace test {
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt | Except.ItsUgly  )]
		interface ITest { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		class Test : ITest {
 
			[Mutability.Unaudited( Because.ItHasntBeenLookedAt )]
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassHasImmutableMemberWithSameExceptions_NoDiag() {
			const string test = @"
	namespace test {
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		class Test {
 
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassHasImmutableMemberWithSubsetOfExceptions_NoDiag() {
			const string test = @"
	namespace test {
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt | Except.ItsUgly )]
		class Test {
 
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test );
		}

		[Test]
		public void DocumentWithStatic_ClassHasImmutableMemberWithProperSupersetOfExceptions_Diag() {
			const string test = @"
	namespace test {
		[Immutable( Except = Except.ItHasntBeenLookedAt | Except.ItsUgly )]
		sealed class Foo { }
 
		[Immutable( Except = Except.ItHasntBeenLookedAt )]
		class Test {
 
			public readonly Foo bad = default( Foo );
 
		}
	}";
			VerifyCSharpDiagnostic( s_preamble + test, new[] {
				new DiagnosticResult {
					Id = Diagnostics.InvalidUnauditedReasonInImmutable.Id,
					Message = "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or relax exceptions on this type to a superset of { ItHasntBeenLookedAt, ItsUgly }.",
					Locations = new [] {
						new DiagnosticResultLocation( "Test0.cs", line: 46, column: 9 )
					},
					Severity = DiagnosticSeverity.Error
				}
			} );
		}

		#endregion

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( string file, int line, int column, MutabilityInspectionResult result ) {
			var reason = m_formatter.Format( result );
			var message = string.Format( Diagnostics.ImmutableClassIsnt.MessageFormat.ToString(), reason );

			var expected = new DiagnosticResult {
				Id = Diagnostics.ImmutableClassIsnt.Id,
				Message = message,
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};

			VerifyCSharpDiagnostic( file, expected );
		}
	}
}
