// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Logging.LoggingExecutionContextScopeBuilderAnalyzer

namespace D2L.LP.Logging.ExecutionContexts {
	using System.Threading.Tasks;

	public interface ILoggingExecutionContextScopeBuilder {
		ILoggingExecutionContextScopeBuilder With( string key, object value );

		void Run( Action action );
		TResult Run<TResult>( Func<TResult> action );

		Task RunAsync( Func<Task> action );
		Task<TResult> RunAsync<TResult>( Func<Task<TResult>> action );
	}
}

namespace D2L.CodeStyle.Analyzers.Specs {

	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using D2L.LP.Logging.ExecutionContexts;

	internal sealed class Foo {

		private readonly ILoggingExecutionContextScopeBuilder m_builder;

		public void ReportedCalls() {
			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( TaskAction );
			{ Task t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( TaskAction ); }
			{ Task t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<Task> /**/( TaskAction ); }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( TaskTAction );
			{ Task<int> t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( TaskTAction ); }
			{ Task<int> t =  m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<Task<int>> /**/( TaskTAction ) ; }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( async () => { } ) ;
			{ Task t =  m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( async () => { } ) ; }
			{ Task t =  m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<Task> /**/( async () => { } ) ; }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( async () => 0 ) ;
			{ Task<int> t =  m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( async () => 0 ) ; }
			{ Task<int> t =  m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<Task<int>> /**/( async () => 0 ) ; }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( ConfiguredTaskAction );
			{ ConfiguredTaskAwaitable t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( ConfiguredTaskAction ); }
			{ ConfiguredTaskAwaitable t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<ConfiguredTaskAwaitable> /**/( ConfiguredTaskAction ); }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( ConfiguredTaskTAction );
			{ ConfiguredTaskAwaitable<int> t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( ConfiguredTaskTAction ); }
			{ ConfiguredTaskAwaitable<int> t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<ConfiguredTaskAwaitable<int>> /**/( ConfiguredTaskTAction ); }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( () => TaskAction().ConfigureAwait( false ) );
			{ ConfiguredTaskAwaitable t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( TaskAction().ConfigureAwait( false ) ); }
			{ ConfiguredTaskAwaitable t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<ConfiguredTaskAwaitable> /**/( TaskAction().ConfigureAwait( false ) ); }

			m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( () => TaskTAction().ConfigureAwait( false ) );
			{ ConfiguredTaskAwaitable<int> t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run /**/( () => TaskTAction().ConfigureAwait( false ) ); }
			{ ConfiguredTaskAwaitable<int> t = m_builder./* LoggingContextRunAwaitable(Use RunAsync for awaitable actions.) */ Run<ConfiguredTaskAwaitable<int>> /**/( () => TaskTAction().ConfigureAwait( false ) ); }
		}

		public void WouldBeNice() {
			T Helper<T>( Action<T> action ) {
				return m_builder.Run( action );
			};

			Helper( TaskAction );
			{ Task t = Helper( TaskAction ); }
			{ Task t = Helper<Task>( TaskAction ); }

			Helper( TaskTAction );
			{ Task<int> t = Helper( TaskTAction ); }
			{ Task<int> t = Helper<Task<int>>( TaskTAction ); }
		}

		public void OtherCalls() {
			m_builder.Run( VoidAction );

			m_builder.Run( IntAction );
			{ int i = m_builder.Run( IntAction ); }
			{ int i = m_builder.Run<int>( IntAction ); }

			m_builder.RunAsync( TaskAction );
			{ Task t = m_builder.RunAsync( TaskAction ); }

			m_builder.RunAsync( TaskTAction );
			{ Task<int> i = m_builder.RunAsync( TaskTAction ); }
			{ Task<int> i = m_builder.RunAsync<int>( TaskTAction ); }

			m_builder.RunAsync( async () => { } );
			{ Task t = m_builder.RunAsync( async () => { } ); }

			m_builder.RunAsync( async () => 0 );
			{ Task<int> i = m_builder.RunAsync( async () => 0 ); }
			{ Task<int> i = m_builder.RunAsync<int>( async () => 0 ); }
		}

		private Task TaskAction() { }
		private Task<int> TaskTAction() { }
		private ConfiguredTaskAwaitable ConfiguredTaskAction() { }
		private ConfiguredTaskAwaitable<int> ConfiguredTaskTAction() { }

		private void VoidAction() { }
		private int IntAction() { }

	}
}
