// analyzer: D2L.CodeStyle.Analyzers.Language.AwaitedTasksAnalyzer

using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks {

	public struct ValueTask {
		public ConfiguredValueTaskAwaitable ConfigureAwait( bool continueOnCapturedContext ) { }
	}
	public struct ValueTask<TResult> {
		public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait( bool continueOnCapturedContext ) { }
	}

}

namespace System.Runtime.CompilerServices {

	public struct ConfiguredValueTaskAwaitable { }
	public struct ConfiguredValueTaskAwaitable<TResult> { }

}

namespace SpecTests {
	public sealed class SpecTests<T> {

		async public Task Good() {

			await TaskReturningFunction().ConfigureAwait( false );
			await TaskTReturningFunction().ConfigureAwait( false );
			await SomeClass.TaskReturningMemberFunction().ConfigureAwait( false );
			await SomeClass.TaskTReturningMemberFunction().ConfigureAwait( false );
			await ValueTaskReturningFunction().ConfigureAwait( false );
			await ValueTaskTReturningFunction().ConfigureAwait( false );
			await SomeClass.ValueTaskReturningMemberFunction().ConfigureAwait( false );
			await SomeClass.ValueTaskTReturningMemberFunction().ConfigureAwait( false );

			await ConfiguredTaskReturningFunction();
			await ConfiguredTaskTReturningFunction();
			await ConfiguredValueTaskReturningFunction();
			await ConfiguredValueTaskTReturningFunction();

			{
				Task t = default;
				await t.ConfigureAwait( false );
			}

			{
				Task<T> t = default;
				await t.ConfigureAwait( false );
			}

			{
				ValueTask t = default;
				await t.ConfigureAwait( false );
			}

			{
				ValueTask<T> t = default;
				await t.ConfigureAwait( false );
			}

		}

		async public Task IdeallyBadBecauseConfiguredWrong() {

			await TaskReturningFunction().ConfigureAwait( true );
			await TaskTReturningFunction().ConfigureAwait( true );
			await SomeClass.TaskReturningMemberFunction().ConfigureAwait( true );
			await SomeClass.TaskTReturningMemberFunction().ConfigureAwait( true );
			await ValueTaskReturningFunction().ConfigureAwait( true );
			await ValueTaskTReturningFunction().ConfigureAwait( true );
			await SomeClass.ValueTaskReturningMemberFunction().ConfigureAwait( true );
			await SomeClass.ValueTaskTReturningMemberFunction().ConfigureAwait( true );

			{
				Task t = default;
				await t.ConfigureAwait( true );
			}

			{
				Task<T> t = default;
				await t.ConfigureAwait( true );
			}

			{
				ValueTask t = default;
				await t.ConfigureAwait( true );
			}

			{
				ValueTask<T> t = default;
				await t.ConfigureAwait( true );
			}

		}

		async public Task BadBecauseNotConfigured() {

			/* AwaitedTaskNotConfigured() */ await TaskReturningFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await TaskTReturningFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await SomeClass.TaskReturningMemberFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await SomeClass.TaskTReturningMemberFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await ValueTaskReturningFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await ValueTaskTReturningFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await SomeClass.ValueTaskReturningMemberFunction() /**/;
			/* AwaitedTaskNotConfigured() */ await SomeClass.ValueTaskTReturningMemberFunction() /**/;

			{
				Task t = default;
				/* AwaitedTaskNotConfigured() */ await t /**/;
			}

			{
				Task<T> t = default;
				/* AwaitedTaskNotConfigured() */ await t /**/;
			}

			{
				ValueTask t = default;
				/* AwaitedTaskNotConfigured() */ await t /**/;
			}

			{
				ValueTask<T> t = default;
				/* AwaitedTaskNotConfigured() */ await t /**/;
			}

		}

		async public Task TaskReturningFunction() { }
		async public Task<T> TaskTReturningFunction() { }
		async public ValueTask ValueTaskReturningFunction() { }
		async public ValueTask<T> ValueTaskTReturningFunction() { }

		async public ConfiguredTaskAwaitable ConfiguredTaskReturningFunction() { }
		async public ConfiguredTaskAwaitable<T> ConfiguredTaskTReturningFunction() { }
		async public ConfiguredValueTaskAwaitable ConfiguredValueTaskReturningFunction() { }
		async public ConfiguredValueTaskAwaitable<T> ConfiguredValueTaskTReturningFunction() { }

		public static class SomeClass {

			async public static Task TaskReturningMemberFunction() { }
			async public static Task<T> TaskTReturningMemberFunction() { }
			async public static ValueTask ValueTaskReturningMemberFunction() { }
			async public static ValueTask<T> ValueTaskTReturningMemberFunction() { }

		}

	}
}
