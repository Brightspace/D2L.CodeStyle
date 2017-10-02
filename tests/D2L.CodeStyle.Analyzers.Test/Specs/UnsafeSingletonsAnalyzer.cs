// analyzer: D2L.CodeStyle.Analyzers.UnsafeSingletons.UnsafeSingletonsAnalyzer

using System;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.CodeStyle.Annotations {
	public sealed class Objects {
		public sealed class Immutable : Attribute { }
	}
}

// copied from: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/IDependencyRegistry.cs
// and: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
namespace D2L.LP.Extensibility.Activation.Domain {
	public enum ObjectScope {
		AlwaysCreateNewInstance = 0,
		Singleton = 1,
		Thread = 2,
		WebRequest = 3
	}
	public interface IFactory<TDependencyType> { }
	public interface IFactory<TDependencyType, T> { }
	public interface IDependencyRegistry {

		void Register<TDependencyType>(
				TDependencyType instance
			);

		void Register<TDependencyType, TConcreteType>(
				ObjectScope scope
			) where TConcreteType : TDependencyType;

		void Register( Type dependencyType, Type concreteType, ObjectScope scope );

		void RegisterFactory<TDependencyType, TFactoryType>(
				ObjectScope scope
			) where TFactoryType : IFactory<TDependencyType>;

		void RegisterParentAwareFactory<TDependencyType, TFactoryType>()
			where TFactoryType : IFactory<TDependencyType, Type>;

		void RegisterPlugin<TDependencyType>(
				TDependencyType instance
			);

		void RegisterPlugin<TDependencyType, TConcreteType>(
				ObjectScope scope
			) where TConcreteType : TDependencyType;

		void RegisterPluginFactory<TDependencyType, TFactoryType>(
				ObjectScope scope
			) where TFactoryType : IFactory<TDependencyType>;

		void UnhandledRegisterMethod();
	}
}

namespace SpecTests {
	public sealed class SomeTestCases {
		public void DoesntMatter( IDependencyRegistry reg ) {

			// Immutable Singletons are not flagged.
			reg.Register<string>( "hello" );
			reg.Register<ISingleton>( new SafeSingleton() );
			reg.Register( new SafeSingleton() ); // inferred generic argument of above
			reg.RegisterPlugin<ISingleton>( new SafeSingleton() );
			reg.RegisterPlugin( new SafeSingleton() ); // inferred generic argument of above
			reg.Register<ISingleton, SafeSingleton>( ObjectScope.Singleton );
			reg.Register( typeof( ISingleton ), typeof( SafeSingleton ), ObjectScope.Singleton );

			// Mutable Singletons are flagged.
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register<ISingleton>( new UnsafeSingleton() ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register( new UnsafeSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPlugin<ISingleton>( new UnsafeSingleton() ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPlugin( new UnsafeSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register( typeof( ISingleton ), typeof( UnsafeSingleton ), ObjectScope.Singleton ) /**/;
			
			// And factory Singletons or singletons where concrete type is not resolved inspect the interface
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.Register<ISingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterPlugin<ISingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.Register<ISingleton>( null ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterFactory<ISingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterPluginFactory<ISingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;

			// Non-Singletons are not flagged.
			reg.Register( typeof( ISingleton ), typeof( UnsafeSingleton ), ObjectScope.WebRequest );
			reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );
			reg.RegisterPlugin<ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );
			reg.RegisterFactory<ISingleton, SingletonFactory>( ObjectScope.Thread );
			reg.RegisterPluginFactory<ISingleton, SingletonFactory>( ObjectScope.WebRequest );
			reg.RegisterParentAwareFactory<ISingleton, SingletonFactory>();

			// Types that don't exist should raise a diagnostic, so that we can be strict. 
			/* SingletonRegistrationTypeUnknown */ reg.RegisterFactory<NonExistentTypeOrInTheMiddleOfTyping, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* SingletonRegistrationTypeUnknown */ reg.RegisterPluginFactory<NonExistentTypeOrInTheMiddleOfTyping, SingletonFactory>( ObjectScope.Singleton ) /**/;

			// Unhandled registration methods should raise a diagnostic.
			/* RegistrationKindUnknown */ reg.UnhandledRegisterMethod() /**/;
		}

		// Registrations in some classes/structs are ignored because they 
		// are wrappers of other register methods and we don't have enough
		// to analyze.
		public static class RegistrationCallsInThisClassAreIgnored {
			public static void DoesntMatter<T>( IDependencyRegistry reg ) where T : new() {
				reg.Register<T>( new T() );
			}
		}
		public struct RegistrationCallsInThisStructAreIgnored {
			public static void DoesntMatter<T>( IDependencyRegistry reg ) where T : new() {
				reg.Register<T>( new T() );
			}
		}

		// Registrations in extension methods with enough information should be handled. 
		public static class SomeExtensionMethods {
			public static void ThisIsSafe<TDependencyType, TConcreteType>( this IDependencyRegistry reg ) 
				where TDependencyType : IImmutable
				where TConcreteType : TDependencyType {
				reg.Register<TDependencyType, TConcreteType>();
			}
		}
	}

	[Objects.Immutable]
	public interface IImmutable { }

	public interface ISingleton { }

	public sealed class SafeSingleton : ISingleton {
		public readonly int immutableField = 0;
	}

	public sealed class UnsafeSingleton : ISingleton {
		public int mutableField = 0;
	}

	public sealed class SingletonFactory : IFactory<ISingleton>, IFactory<ISingleton, Type> { }

}
