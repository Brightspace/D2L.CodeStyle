using D2L.CodeStyle.Analyzers.Common;
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

		private readonly MutabilityInspectionResultFormatter m_formatter = new MutabilityInspectionResultFormatter();

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			VerifyCSharpDiagnostic( test );
		}

		[Test]
		public void DocumentWithStatic_ClassIsNotImmutableButIsMarkedImmutable_Diag() {
			const string test = @"
	using System;

	namespace test {
		[Immutable]
		class Test {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( test, 6, 9, MutabilityInspectionResult.Mutable(
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
		[Immutable] interface IFoo {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( test, 6, 9, MutabilityInspectionResult.Mutable(
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
		[Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public DateTime bad = DateTime.Now;
			public DateTime badToo { get; set; }

		}
	}";
			AssertSingleDiagnostic( test, 7, 9, MutabilityInspectionResult.Mutable( 
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
		[Immutable] interface IFooBase {} 
		interface IFoo : IFooBase {} 
		class Test : IFoo {

			public readonly DateTime bad = DateTime.Now;
			public DateTime badToo { get; }

		}
	}";
			AssertNoDiagnostic( test );
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
