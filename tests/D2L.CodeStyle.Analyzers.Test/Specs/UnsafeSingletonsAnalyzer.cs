// analyzer: D2L.CodeStyle.Analyzers.UnsafeSingletons.UnsafeSingletonsAnalyzer

using System;
using D2L.LP.Extensibility.Activation.Domain;

// copied from: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/IDependencyRegistry.cs
// and: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
namespace D2L.LP.Extensibility.Activation.Domain {
	public enum ObjectScope {
		AlwaysCreateNewInstance = 0,
		Singleton = 1,
		Thread = 2,
		WebRequest = 3
	}
	public interface IDependencyRegistry {

		void Register<TDependencyType>(
				TDependencyType instance
			);

		void Register<TDependencyType, TConcreteType>(
				ObjectScope scope
			) where TConcreteType : TDependencyType;

		void Register( Type dependencyType, Type concreteType, ObjectScope scope );
	}
}

namespace SpecTests {
	public sealed class SomeTestCases {
		public void DoesntMatter( IDependencyRegistry reg ) {

			// Immutable Singletons are not flagged.
			reg.Register<string>( "hello" );
			reg.Register<ISingleton>( new SafeSingleton() );
			reg.Register( new SafeSingleton() ); // inferred generic argument of above
			reg.Register<ISingleton, SafeSingleton>( ObjectScope.Singleton );
			reg.Register( typeof( ISingleton ), typeof( SafeSingleton ), ObjectScope.Singleton );

			// Mutable Singletons are flagged.
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register( typeof( ISingleton ), typeof( UnsafeSingleton ), ObjectScope.Singleton ) /**/;

			// Non-Singletons are not flagged.
			reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );

			// Concrete types that don't exist should raise a diagnostic, so that we can be strict. 
			/* ConcreteTypeNotResolved */ reg.Register<ISingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
		}
	}

	public interface ISingleton { }

	public sealed class SafeSingleton : ISingleton {
		public readonly int immutableField = 0;
	}

	public sealed class UnsafeSingleton : ISingleton {
		public int mutableField = 0;
	}

}
