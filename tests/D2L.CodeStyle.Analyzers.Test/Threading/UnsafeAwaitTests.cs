using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using D2L.CodeStyle.Analyzers.UnsafeStatics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Threading {

	internal sealed class UnsafeAwaitTests : DiagnosticVerifier {

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new UseConfigureAwaitFalseAnalyzer();
		}

		[Test]
		public void VerifyAnalyzer_HasUnsafeAwait_DiagnosticsShowUp() {

			const string code = @"using System;
using System.Threading;
using System.Threading.Tasks;

public class Test {
	Task<int> DoSomethingAsync() {   
		return Task.FromResult(5);
	}

	async Task<string> DoSomethignElseAsync() {
		int i = await DoSomethingAsync();
		return $""hello {i}th person"";
	}
}";

			AssertSingleDiagnostic(code, 11, 10, "bad", "it");

		}


		private void AssertSingleDiagnostic(string file, int line, int column, string fieldOrProp, string badFieldOrType) {

			DiagnosticResult result = CreateDiagnosticResult(line, column, fieldOrProp, badFieldOrType);
			VerifyCSharpDiagnostic(file, result);
		}

		private static DiagnosticResult CreateDiagnosticResult(int line, int column, string fieldOrProp, string badFieldOrType) {
			return new DiagnosticResult
			{
				Id = UnsafeStaticsAnalyzer.DiagnosticId,
				Message = string.Format(UnsafeStaticsAnalyzer.MessageFormat, fieldOrProp, badFieldOrType),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
