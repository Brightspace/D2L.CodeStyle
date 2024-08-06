// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.NoAsyncFuncsAnalyzer, D2L.CodeStyle.Analyzers

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations.Contract;

namespace Tests;

public sealed class Receiver {

	public static void RunUnblocked<T>( Func<T> fn ) { }

	public static void Run<T>( [NoAsyncFuncs( Message = $"Use {nameof( RunAsync )} instead" )] Func<T> fn ) { }
	public static void Run2<T, U>( [NoAsyncFuncs] Func<T> fn, [NoAsyncFuncs] Func<U> fn2 ) { }

	public static void RunAsync( Func<Task> fn ) { }
	public static void RunAsync<T>( Func<Task<T>> fn ) { }

}

public sealed class Foo {

	public void ReportedCalls() {
		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ TaskAction /**/ );
		{ Task t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ TaskAction /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ TaskTAction /**/ );
		{ Task<int> t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ TaskTAction /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ async () => { } /**/ );
		{ Task t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ async () => { } /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ async () => 0 /**/ );
		{ Task<int> t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ async () => 0 /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ ConfiguredTaskAction /**/ );
		{ ConfiguredTaskAwaitable t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ ConfiguredTaskAction /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ ConfiguredTaskTAction /**/ );
		{ ConfiguredTaskAwaitable<int> t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ ConfiguredTaskTAction /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ () => TaskAction().ConfigureAwait( false ) /**/ );
		{ ConfiguredTaskAwaitable t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ () => TaskAction().ConfigureAwait( false ) /**/ ); }

		Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ () => TaskTAction().ConfigureAwait( false ) /**/ );
		{ ConfiguredTaskAwaitable<int> t = Receiver.Run( /* AsyncFuncsBlocked(. Use RunAsync instead) */ () => TaskTAction().ConfigureAwait( false ) /**/ ); }


		Receiver.Run2( /* AsyncFuncsBlocked() */ TaskAction /**/, /* AsyncFuncsBlocked() */ TaskAction /**/ );
		Receiver.Run2( IntAction, /* AsyncFuncsBlocked() */ TaskAction /**/ );
		Receiver.Run2( /* AsyncFuncsBlocked() */ TaskAction /**/, IntAction );
	}

	public void WouldBeNice() {
		T Helper<T>( Action<T> action ) {
			return Receiver.Run( action );
		};

		Helper( TaskAction );
		{ Task t = Helper( TaskAction ); }
		{ Task t = Helper<Task>( TaskAction ); }

		Helper( TaskTAction );
		{ Task<int> t = Helper( TaskTAction ); }
		{ Task<int> t = Helper<Task<int>>( TaskTAction ); }
	}

	public void OtherCalls() {
		Receiver.RunUnblocked( TaskAction );

		Receiver.Run( IntAction );
		Receiver.Run2( IntAction, IntAction );
		{ int i = Receiver.Run( IntAction ); }

		Receiver.RunAsync( TaskAction );
		{ Task t = Receiver.RunAsync( TaskAction ); }

		Receiver.RunAsync( TaskTAction );
		{ Task<int> i = Receiver.RunAsync( TaskTAction ); }

		Receiver.RunAsync( async () => { } );
		{ Task t = Receiver.RunAsync( async () => { } ); }

		Receiver.RunAsync( async () => 0 );
		{ Task<int> i = Receiver.RunAsync( async () => 0 ); }
	}

	private Task TaskAction() { }
	private Task<int> TaskTAction() { }
	private ConfiguredTaskAwaitable ConfiguredTaskAction() { }
	private ConfiguredTaskAwaitable<int> ConfiguredTaskTAction() { }

	private int IntAction() { }

}
