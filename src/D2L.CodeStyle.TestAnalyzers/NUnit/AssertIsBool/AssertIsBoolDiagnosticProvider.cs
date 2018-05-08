using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal sealed class AssertIsBoolDiagnosticProvider<T> where T : ExpressionSyntax {

		private readonly Func<T, string> m_isTrueDiagnostic;
		private readonly Func<T, string> m_isFalseDiagnostic;

		public AssertIsBoolDiagnosticProvider(
			Func<T, string> isTrueDiagnostic,
			Func<T, string> isFalseDiagnostic
		) {
			m_isTrueDiagnostic = isTrueDiagnostic;
			m_isFalseDiagnostic = isFalseDiagnostic;
		}

		public AssertIsBoolDiagnosticProvider<T> Opposite() =>
			new AssertIsBoolDiagnosticProvider<T>( m_isFalseDiagnostic, m_isTrueDiagnostic );

		public Func<T, string> GetDiagnosticFunc( string symbolName ) {
			Func<T, string> getDiagnosticFunc;
			switch( symbolName ) {
				case AssertIsBoolAnalyzer.AssertIsTrue:
					getDiagnosticFunc = m_isTrueDiagnostic;
					break;
				case AssertIsBoolAnalyzer.AssertIsFalse:
					getDiagnosticFunc = m_isFalseDiagnostic;
					break;
				default:
					throw new InvalidOperationException( $"unknown '{symbolName}' symbol" );
			}

			return getDiagnosticFunc;
		}
	}
}
