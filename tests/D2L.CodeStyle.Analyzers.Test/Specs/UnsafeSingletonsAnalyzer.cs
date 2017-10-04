// analyzer: D2L.CodeStyle.Analyzers.UnsafeSingletons.UnsafeSingletonsAnalyzer

using System;
using System.Collections.Generic;
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
	public abstract class ExtensionPointDescriptor {
		public abstract string Name { get; }
	}
	public interface IExtensionPoint<T> { }
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
	public static class ExtensionMethods {
		// from http://search.dev.d2l/source/xref/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/DynamicObjectFactories/DynamicObjectFactoryRegistryExtensions.cs
		public static void RegisterDynamicObjectFactory<TOutput, TConcrete, TArg>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) where TConcrete : class, TOutput;
		public static void RegisterDynamicObjectFactory<TOutput, TConcrete, TArg0, TArg1>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) where TConcrete : class, TOutput;

		// from: http://search.dev.d2l/source/xref/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/IDependencyRegistryConfigurePluginsExtensions.cs
		public static void ConfigurePlugins<TPlugin>(
				this IDependencyRegistry registry,
				ObjectScope scope
			);
		public static void ConfigureOrderedPlugins<TPlugin, TComparer>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) where TComparer : IComparer<TPlugin>, new();

		// from: http://search.dev.d2l/source/xref/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/DependencyRegistryExtensionPointExtensions.cs
		public static void RegisterPluginExtensionPoint<TExtensionPoint, T>(
				this IDependencyRegistry @this,
				ObjectScope scope
			) where TExtensionPoint : IExtensionPoint<T>;
		public static void RegisterPlugin<TExtensionPoint, TDependencyType, TConcreteType>(
				this IDependencyRegistry @this,
				ObjectScope scope
			)
			where TConcreteType : TDependencyType
			where TExtensionPoint : IExtensionPoint<TDependencyType>;
		public static void RegisterPluginFactory<TExtensionPoint, TDependencyType, TFactoryType>(
				this IDependencyRegistry @this,
				ObjectScope scope
			)
			where TFactoryType : IFactory<TDependencyType>
			where TExtensionPoint : IExtensionPoint<TDependencyType>;

		// from: http://search.dev.d2l/source/xref/Lms/core/lp/framework/core/D2L.LP/LP/Extensibility/Plugins/DI/LegacyPluginsDependencyLoaderExtensions.cs
		public static void ConfigureInstancePlugins<TPlugin>(
				this IDependencyRegistry registry,
				ObjectScope scope
			);
		public static void ConfigureInstancePlugins<TPlugin, TExtensionPoint>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) where TExtensionPoint : ExtensionPointDescriptor, new();
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
			reg.ConfigurePlugins<SafeSingleton>( ObjectScope.Singleton );
			reg.ConfigureOrderedPlugins<SafeSingleton, SomeComparer<SafeSingleton>>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<SafeSingleton>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<SafeSingleton, DefaultExtensionPoint<SafeSingleton>>( ObjectScope.Singleton );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<SafeSingleton>, SafeSingleton>( ObjectScope.Singleton );
			reg.RegisterPlugin<DefaultExtensionPoint<SafeSingleton>, ISingleton, SafeSingleton>( ObjectScope.Singleton );

			// Mutable Singletons are flagged.
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register<ISingleton>( new UnsafeSingleton() ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register( new UnsafeSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPlugin<ISingleton>( new UnsafeSingleton() ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPlugin( new UnsafeSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.Register( typeof( ISingleton ), typeof( UnsafeSingleton ), ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.ConfigurePlugins<UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.ConfigureOrderedPlugins<UnsafeSingleton, SomeComparer<UnsafeSingleton>>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.ConfigureInstancePlugins<UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.ConfigureInstancePlugins<UnsafeSingleton, DefaultExtensionPoint<UnsafeSingleton>>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<UnsafeSingleton>, UnsafeSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.UnsafeSingleton,'mutableField' is not read-only) */ reg.RegisterPlugin<DefaultExtensionPoint<UnsafeSingleton>, ISingleton, UnsafeSingleton>( ObjectScope.Singleton ) /**/;

			// And factory Singletons or singletons where concrete type is not resolved inspect the interface
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.Register<ISingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterPlugin<ISingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.Register<ISingleton>( null ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterFactory<ISingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterPluginFactory<ISingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterPluginFactory<DefaultExtensionPoint<SafeSingleton>, ISingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterDynamicObjectFactory<ISingleton, SafeSingleton, string, string>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonField(SpecTests.ISingleton,its type ('SpecTests.ISingleton') is an interface that is not marked with `[Objects.Immutable]`) */ reg.RegisterDynamicObjectFactory<ISingleton, SafeSingleton, string>( ObjectScope.Singleton ) /**/;

			// Non-Singletons are not flagged.
			reg.Register( typeof( ISingleton ), typeof( UnsafeSingleton ), ObjectScope.WebRequest );
			reg.Register<ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );
			reg.RegisterPlugin<ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );
			reg.RegisterFactory<ISingleton, SingletonFactory>( ObjectScope.Thread );
			reg.RegisterPluginFactory<ISingleton, SingletonFactory>( ObjectScope.WebRequest );
			reg.RegisterParentAwareFactory<ISingleton, SingletonFactory>();
			reg.ConfigurePlugins<UnsafeSingleton>( ObjectScope.WebRequest );
			reg.ConfigureOrderedPlugins<UnsafeSingleton, SomeComparer<UnsafeSingleton>>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<UnsafeSingleton>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<UnsafeSingleton, DefaultExtensionPoint<UnsafeSingleton>>( ObjectScope.WebRequest );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<UnsafeSingleton>, UnsafeSingleton>( ObjectScope.WebRequest );
			reg.RegisterPlugin<DefaultExtensionPoint<UnsafeSingleton>, ISingleton, UnsafeSingleton>( ObjectScope.WebRequest );

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

		public static class OnlyRegistrationMethodsOnIDependencyRegistryMatter {
			public static void DoesntMatter( IDependencyRegistry reg ) {
				Register<UnsafeSingleton>( null );
			}
			public static void Register<TDependencyType>(
				TDependencyType instance
			);
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

	public sealed class DefaultExtensionPoint<T> : ExtensionPointDescriptor, IExtensionPoint<T> {
		public override string Name { get; } = "Default";
	}
	public sealed class SomeComparer<T> : IComparer<T> {
		int IComparer<T>.Compare( T x, T y ) {
			throw new NotImplementedException();
		}
	}
	public sealed class SingletonFactory : IFactory<ISingleton>, IFactory<ISingleton, Type> { }

}
