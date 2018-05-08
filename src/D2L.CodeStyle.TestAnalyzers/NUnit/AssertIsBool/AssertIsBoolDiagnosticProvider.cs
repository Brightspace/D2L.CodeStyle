using System;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal sealed class AssertIsBoolDiagnosticProvider {

		private const string AssertIsTrue = "NUnit.Framework.Assert.IsTrue";
		private const string AssertIsFalse = "NUnit.Framework.Assert.IsFalse";

		public static bool CanDiagnoseSymbol( string symbolName ) {
			return symbolName == AssertIsTrue || symbolName == AssertIsFalse;
		}

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

		public Func<string> GetDiagnosticFunc( string symbolName ) {
			Func<string> getDiagnosticFunc;
			switch( symbolName ) {
				case AssertIsTrue:
					getDiagnosticFunc = m_isTrueDiagnostic;
					break;
				case AssertIsFalse:
					getDiagnosticFunc = m_isFalseDiagnostic;
					break;
				default:
					throw new InvalidOperationException( $"unknown '{symbolName}' symbol" );
			}

			return getDiagnosticFunc;
		}
	}
}
