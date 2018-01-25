using D2L.CodeStyle.Analyzers.Immutability;
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
using D2L.CodeStyle.Annotations;
namespace D2L.CodeStyle.Annotations {
	public class Objects {
		public class Immutable : Attribute {}
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
	using System;

	namespace test {
		[Objects.Immutable]
		class Test {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 13, 9, MutabilityInspectionResult.Mutable(
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}


		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButImplementsImmutableInterface_Diag() {
			const string test = @"
	using System;

	namespace test {
		[Objects.Immutable] interface IFoo {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 13, 9, MutabilityInspectionResult.Mutable(
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}

		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButImplementsImmutableInterfaceInChain_Diag() {
			const string test = @"
	using System;

	namespace test {
		[Objects.Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( s_preamble + test, 14, 9, MutabilityInspectionResult.Mutable( 
				"bad",
				"System.DateTime",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			) );
		}

		[Test]
		public void DocumentWithStatic_ClassIsImmutableAndImplementsImmutableInterface_NoDiag() {
			const string test = @"
	using System;

	namespace test {
		[Objects.Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public readonly DateTime bad = DateTime.Now;
			public DateTime badToo { get; }

		}
	}";
			AssertNoDiagnostic( s_preamble + test );
		}

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
