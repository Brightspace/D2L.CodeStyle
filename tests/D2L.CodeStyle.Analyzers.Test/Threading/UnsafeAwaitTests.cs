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
		public void VerifyAnalyzer_HasMultipleUnsafeAwaits_MultipleDiagnostics() {
			const string code = @"using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public static class TaskExtensions {
    public static ConfiguredTaskAwaitable SafeAsync( this Task @this ) {
        return @this.ConfigureAwait( continueOnCapturedContext: false );
    }

    public static ConfiguredTaskAwaitable<T> SafeAsync<T>( this Task<T> @this ) {
        return @this.ConfigureAwait( continueOnCapturedContext: false );
    }
}

public class Test {
    Task<int> DoSomethingAsync() {   
        return Task.FromResult(5);
    }

    async Task NonGenericDoSomethingAsync() {   
        await Task.FromResult(5);
    }

    async Task<string> DoSomethignElseAsync() {
        int i = await DoSomethingAsync().SafeAsync();
        await NonGenericDoSomethingAsync();
	    Task a = Task.Factory.StartNew(
                () => {
                    for( int i = 0; i < 1000000; i++ ) {
                        Console.WriteLine(i);
                    }
                } );
		await a;


        return $""""hello {i}th person"""";
    }
}";
			var diag1 = CreateDiagnosticResult(22, 9);
			var diag2 = CreateDiagnosticResult(27, 9);
			var diag3 = CreateDiagnosticResult(34, 3);
			VerifyCSharpDiagnostic(code, diag1, diag2, diag3);
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
		int j = await DoSomethingAsync().ConfigureAwait(false);
		return $""hello {i}th person"";
	}
}";

			AssertSingleDiagnostic( code, 11, 11 );

		}

		[Test]
		public void VerifyAnalyzer_HasSafeAwait_DiagnosticsShowDoNotShowUp() {

			const string code = @"using System;
using System.Threading;
using System.Threading.Tasks;

public class Test {
	Task<int> DoSomethingAsync() {   
		return Task.FromResult(5);
	}

	Task NonGenericDoSomethingAsync() {   
		return Task.FromResult(5);
	}

	async Task<string> DoSomethignElseAsync() {
		int i = await DoSomethingAsync().ConfigureAwait(false);
		await NonGenericDoSomethingAsync().ConfigureAwait(false);
		return $""hello {i}th person"";
	}
}";

			VerifyCSharpDiagnostic( code );

		}

		[Test]
		public void VerifyAnalyzer_HasSafeAwaitWithTrue_DiagnosticsShowDoNotShowUp() {

			const string code = @"using System;
using System.Threading;
using System.Threading.Tasks;

public class Test {
	Task<int> DoSomethingAsync() {   
		return Task.FromResult(5);
	}

	Task NonGenericDoSomethingAsync() {   
		return Task.FromResult(5);
	}

	async Task<string> DoSomethignElseAsync() {
		int i = await DoSomethingAsync().ConfigureAwait(true);
		await NonGenericDoSomethingAsync().ConfigureAwait(true);
		return $""hello {i}th person"";
	}
}";

			VerifyCSharpDiagnostic(code);

		}

		[Test]
		public void VerifyAnalyzer_HasSafeAwaitWithTaskExtensions_DiagnosticsShowDoNotShowUp() {

			const string code = @"using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public static class TaskExtensions {
	public static ConfiguredTaskAwaitable SafeAsync( this Task @this ) {
		return @this.ConfigureAwait( continueOnCapturedContext: false );
	}

	public static ConfiguredTaskAwaitable<T> SafeAsync<T>( this Task<T> @this ) {
		return @this.ConfigureAwait( continueOnCapturedContext: false );
	}
}

public class Test {
	Task<int> DoSomethingAsync() {   
		return Task.FromResult(5);
	}

	Task NonGenericDoSomethingAsync() {   
		return Task.FromResult(5);
	}

	async Task<string> DoSomethignElseAsync() {
		int i = await DoSomethingAsync().SafeAsync();
		await NonGenericDoSomethingAsync().SafeAsync();
		return $""hello {i}th person"";
	}
}";

			VerifyCSharpDiagnostic(code);

		}


		private void AssertSingleDiagnostic(string file, int line, int column) {

			DiagnosticResult result = CreateDiagnosticResult(line, column);
			VerifyCSharpDiagnostic(file, result);
		}

		private static DiagnosticResult CreateDiagnosticResult(int line, int column) {
			return new DiagnosticResult
			{
				Id = UseConfigureAwaitFalseAnalyzer.DiagnosticId,
				Message = "Awaitable should use ConfigureAwait(false) if possible",
				Severity = DiagnosticSeverity.Warning,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};
		}
	}
}
