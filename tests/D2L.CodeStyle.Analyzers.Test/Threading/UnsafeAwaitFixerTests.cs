using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2L.CodeStyle.Analyzers.Verifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Threading {

	internal sealed class UnsafeAwaitFixerTests : CodeFixVerifier {

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new UseConfigureAwaitFalseAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider() {
			return new UseConfigureAwaitFalseAnalyzerFixer();
		}

		[Test]
		public void FixAwait_CodeHasMultipleAwaitIssues_ReturnsCodeWithConfigureAwaitFalse() {
			const string oldCode = @"using System;
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

			const string newCode = @"using System;
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
        await Task.FromResult(5).ConfigureAwait(false);
    }

    async Task<string> DoSomethignElseAsync() {
        int i = await DoSomethingAsync().SafeAsync();
        await NonGenericDoSomethingAsync().ConfigureAwait(false);
        Task a = Task.Factory.StartNew(
                () => {
                    for( int i = 0; i < 1000000; i++ ) {
                        Console.WriteLine(i);
                    }
                } );
        await a.ConfigureAwait(false);
        return $""""hello {i}th person"""";
    }
}";

			VerifyFix( oldCode, newCode);

		}

	}
}
