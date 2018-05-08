using System;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal sealed class AssertIsBoolDiagnosticProvider {

		private readonly Func<string> m_isTrueDiagnostic;
		private readonly Func<string> m_isFalseDiagnostic;

		public AssertIsBoolDiagnosticProvider(
			Func<string> isTrueDiagnostic,
			Func<string> isFalseDiagnostic
		) {
			m_isTrueDiagnostic = isTrueDiagnostic;
			m_isFalseDiagnostic = isFalseDiagnostic;
		}

		public AssertIsBoolDiagnosticProvider Opposite() =>
			new AssertIsBoolDiagnosticProvider( m_isFalseDiagnostic, m_isTrueDiagnostic );

		public string GetDiagnostic( string symbolName ) {
			Func<string> getDiagnosticFunc;
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
