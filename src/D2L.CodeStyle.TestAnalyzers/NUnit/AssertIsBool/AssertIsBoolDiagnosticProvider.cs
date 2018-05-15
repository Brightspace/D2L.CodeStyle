using System;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal sealed class AssertIsBoolDiagnosticProvider {

		private readonly Func<AssertIsBoolDiagnostic> m_isTrueDiagnostic;
		private readonly Func<AssertIsBoolDiagnostic> m_isFalseDiagnostic;

		public AssertIsBoolDiagnosticProvider(
			Func<AssertIsBoolDiagnostic> isTrueDiagnostic,
			Func<AssertIsBoolDiagnostic> isFalseDiagnostic
		) {
			m_isTrueDiagnostic = isTrueDiagnostic;
			m_isFalseDiagnostic = isFalseDiagnostic;
		}

		public AssertIsBoolDiagnosticProvider Opposite() =>
			new AssertIsBoolDiagnosticProvider( m_isFalseDiagnostic, m_isTrueDiagnostic );

		public AssertIsBoolDiagnostic GetDiagnostic( string symbolName ) {
			Func<AssertIsBoolDiagnostic> getDiagnosticFunc;
			switch( symbolName ) {
				case AssertIsBoolSymbols.IsTrue:
					getDiagnosticFunc = m_isTrueDiagnostic;
					break;
				case AssertIsBoolSymbols.IsFalse:
					getDiagnosticFunc = m_isFalseDiagnostic;
					break;
				default:
					throw new InvalidOperationException( $"unknown '{symbolName}' symbol" );
			}

			return getDiagnosticFunc();
		}
	}
}
