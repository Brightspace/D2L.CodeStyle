// analyzer: D2L.CodeStyle.Analyzers.Async.ExplicitCancellationTokenAnalyzer

using System.Threading;
using System.Threading.Tasks;

using static AsyncFunctions;

public static class AsyncFunctions {

	public static Task JumpAsync() {
		return Task.CompletedTask;
	}

	public static Task RunAsync( CancellationToken cancellationToken ) {
		return Task.Delay( 0, cancellationToken );
	}

	public static Task SkipAsync( CancellationToken cancellationToken = default ) {
		return Task.Delay( 0, cancellationToken );
	}

	public static Task SleepAsync( int ms, CancellationToken cancellationToken = default ) {
		return Task.Delay( ms, cancellationToken );
	}
}

public static class Test {

	public static async Task Cases() {
		using CancellationTokenSource cts = new CancellationTokenSource();

		await JumpAsync();

		await RunAsync( default );
		await RunAsync( CancellationToken.None );
		await RunAsync( cts.Token );

		await /* ExplicitCancellationTokenArgumentRequired */ SkipAsync /**/ ();
		await SkipAsync( CancellationToken.None );
		await SkipAsync( cts.Token );

		await /* ExplicitCancellationTokenArgumentRequired */ SleepAsync /**/ ( 1 );
		await SleepAsync( 2, CancellationToken.None );
		await SleepAsync( 3, cts.Token );
	}
}
