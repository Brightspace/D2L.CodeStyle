// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.DependencyRegistrationsAnalyzer

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
	public sealed class SingletonAttribute : Attribute { }
	public sealed class DependencyAttribute : Attribute { }
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
	public interface IFactory<out TDependencyType> { }
	public interface IFactory<out TDependencyType, T> { }
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
		public static void RegisterSubInterface<TSubInterfaceType, TInjectableSuperInterfaceType>(
				this IDependencyRegistry @this,
				ObjectScope scope
			) where TInjectableSuperInterfaceType : TSubInterfaceType;
	}
}

namespace SpecTests {
	public sealed class SomeTestCases {
		public void DoesntMatter( IDependencyRegistry reg ) {

			// Marked Singletons are not flagged.
			reg.Register<IMarkedSingleton>( new MarkedSingleton() );
			reg.Register( new MarkedSingleton() ); // inferred generic argument of above
			reg.RegisterPlugin<IMarkedSingleton>( new MarkedSingleton() );
			reg.RegisterPlugin( new MarkedSingleton() ); // inferred generic argument of above
			reg.Register<ISingleton, MarkedSingleton>( ObjectScope.Singleton );
			reg.RegisterFactory<ISingleton, ConcreteSingletonFactory>( ObjectScope.Singleton );
			reg.RegisterPluginFactory<ISingleton, ConcreteSingletonFactory>( ObjectScope.Singleton );
			reg.Register( typeof( IMarkedSingleton ), typeof( MarkedSingleton ), ObjectScope.Singleton );
			reg.ConfigurePlugins<MarkedSingleton>( ObjectScope.Singleton );
			reg.ConfigureOrderedPlugins<MarkedSingleton, SomeComparer<MarkedSingleton>>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<MarkedSingleton>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<MarkedSingleton, DefaultExtensionPoint<MarkedSingleton>>( ObjectScope.Singleton );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<MarkedSingleton>, MarkedSingleton>( ObjectScope.Singleton );
			reg.RegisterPlugin<DefaultExtensionPoint<MarkedSingleton>, IMarkedSingleton, MarkedSingleton>( ObjectScope.Singleton );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMarkedThing, string>( ObjectScope.Singleton );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMarkedThing, string, string>( ObjectScope.Singleton );
			reg.RegisterSubInterface<ISingleton, IMarkedSingleton>( ObjectScope.Singleton );

			// Unmarked Singletons are flagged.
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.Register<IUnmarkedSingleton>( new UnmarkedSingleton() ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterFactory<IUnmarkedSingleton, ConcreteSingletonFactory>( ObjectScope.Singleton ) /**/; // generic parameter from ConcreteSingletonFactory
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.Register( new UnmarkedSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterPlugin<IUnmarkedSingleton>( new UnmarkedSingleton() ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterPlugin( new UnmarkedSingleton() ) /**/; // inferred generic argument of above
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterPluginFactory<IUnmarkedSingleton, ConcreteSingletonFactory>( ObjectScope.Singleton ) /**/; // generic parameter from ConcreteSingletonFactory
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.Register<IUnmarkedSingleton, UnmarkedSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.Register( typeof( IUnmarkedSingleton ), typeof( UnmarkedSingleton ), ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.ConfigurePlugins<UnmarkedSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.ConfigureOrderedPlugins<UnmarkedSingleton, SomeComparer<UnmarkedSingleton>>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.ConfigureInstancePlugins<UnmarkedSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.ConfigureInstancePlugins<UnmarkedSingleton, DefaultExtensionPoint<UnmarkedSingleton>>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<UnmarkedSingleton>, UnmarkedSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.UnmarkedSingleton) */ reg.RegisterPlugin<DefaultExtensionPoint<UnmarkedSingleton>, IUnmarkedSingleton, UnmarkedSingleton>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.ISubUnmarkedSingleton) */ reg.RegisterSubInterface<IUnmarkedSingleton, ISubUnmarkedSingleton>( ObjectScope.Singleton ) /**/;

			// And factory Singletons or singletons where concrete type is not resolved inspect the interface
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.Register<IUnmarkedSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterPlugin<IUnmarkedSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.Register<IUnmarkedSingleton>( null ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterFactory<IUnmarkedSingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterPluginFactory<IUnmarkedSingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterPluginFactory<DefaultExtensionPoint<UnmarkedSingleton>, IUnmarkedSingleton, SingletonFactory>( ObjectScope.Singleton ) /**/;

			// Dynamic object factory registrations inspect the concrete object's ctor parameters
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaUnmarkedThing, string, string>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.IUnmarkedSingleton) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaUnmarkedThing, string>( ObjectScope.Singleton ) /**/;

			// Dyanamic object factory registrations that error out inspect IFactory<TDependencyType>
			/* UnsafeSingletonRegistration(D2L.LP.Extensibility.Activation.Domain.IFactory<SpecTests.ICreatedByDynamicFactory>) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor, string>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(D2L.LP.Extensibility.Activation.Domain.IFactory<SpecTests.ICreatedByDynamicFactory>) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor, string, string>( ObjectScope.Singleton ) /**/;

			// Unmarked non-Singletons are not flagged.
			reg.Register( typeof( IUnmarkedSingleton ), typeof( UnmarkedSingleton ), ObjectScope.WebRequest );
			reg.Register<IUnmarkedSingleton, UnmarkedSingleton>( ObjectScope.WebRequest );
			reg.RegisterPlugin<IUnmarkedSingleton, UnmarkedSingleton>( ObjectScope.WebRequest );
			reg.RegisterFactory<IUnmarkedSingleton, SingletonFactory>( ObjectScope.Thread );
			reg.RegisterPluginFactory<IUnmarkedSingleton, SingletonFactory>( ObjectScope.WebRequest );
			reg.RegisterParentAwareFactory<IUnmarkedSingleton, SingletonFactory>();
			reg.ConfigurePlugins<UnmarkedSingleton>( ObjectScope.WebRequest );
			reg.ConfigureOrderedPlugins<UnmarkedSingleton, SomeComparer<UnmarkedSingleton>>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<UnmarkedSingleton>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<UnmarkedSingleton, DefaultExtensionPoint<UnmarkedSingleton>>( ObjectScope.WebRequest );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<UnmarkedSingleton>, UnmarkedSingleton>( ObjectScope.WebRequest );
			reg.RegisterPlugin<DefaultExtensionPoint<UnmarkedSingleton>, IUnmarkedSingleton, UnmarkedSingleton>( ObjectScope.WebRequest );

			// Marked non-singletons are flagged.
			/* AttributeRegistrationMismatch(SpecTests.MarkedSingleton) */ reg.Register<IMarkedSingleton, MarkedSingleton>( ObjectScope.WebRequest ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.MarkedSingleton) */ reg.Register<IMarkedSingleton, MarkedSingleton>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.MarkedSingleton) */ reg.Register( typeof( IMarkedSingleton ), typeof( MarkedSingleton ), ObjectScope.WebRequest ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.IMarkedSingleton) */ reg.RegisterFactory<IMarkedSingleton, SingletonFactory>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.MarkedSingleton) */ reg.RegisterPlugin<IMarkedSingleton, MarkedSingleton>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.IMarkedSingleton) */ reg.RegisterPluginFactory<IMarkedSingleton, SingletonFactory>( ObjectScope.WebRequest ) /**/;

			// DynamicObjectFactory registrations at non-Singleton scope are safe,
			// because the implementation is generated, so it will never be marked.
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMarkedThing, string>( ObjectScope.WebRequest );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaUnmarkedThing, string>( ObjectScope.WebRequest );

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
				where TDependencyType : IMarkedSingleton
				where TConcreteType : TDependencyType {
				reg.Register<TDependencyType, TConcreteType>();
			}
		}

		public static class OnlyRegistrationMethodsOnIDependencyRegistryMatter {
			public static void DoesntMatter( IDependencyRegistry reg ) {
				Register<UnmarkedSingleton>( null );
			}
			public static void Register<TDependencyType>(
				TDependencyType instance
			);
		}
	}

	// intentionally unmarked
	public interface ISingleton {}

	[Singleton]
	public interface IMarkedSingleton : ISingleton { }
	public sealed class MarkedSingleton : IMarkedSingleton { }

	public interface IUnmarkedSingleton { }
	public interface ISubUnmarkedSingleton : IUnmarkedSingleton { }
	public sealed class UnmarkedSingleton : IUnmarkedSingleton {}

	public interface ICreatedByDynamicFactory { }

	public class ThingThatIsCreatedByDynamicObjectFactoryViaMarkedThing : ICreatedByDynamicFactory {
		public ThingThatIsCreatedByDynamicObjectFactoryViaMarkedThing(
			string randomArg,
			[Dependency] IMarkedSingleton injected
		) { }
	}

	public class ThingThatIsCreatedByDynamicObjectFactoryViaUnmarkedThing : ICreatedByDynamicFactory {
		public ThingThatIsCreatedByDynamicObjectFactoryViaUnmarkedThing(
			string randomArg,
			[Dependency] IUnmarkedSingleton injected
		) { }
	}

	public class ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor: ICreatedByDynamicFactory {
		private ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor(
			string randomArg
		) { }
	}

	public sealed class DefaultExtensionPoint<T> : ExtensionPointDescriptor, IExtensionPoint<T> {
		public override string Name { get; } = "Default";
	}
	public sealed class SomeComparer<T> : IComparer<T> {
		int IComparer<T>.Compare( T x, T y ) {
			throw new NotImplementedException();
		}
	}
	public sealed class SingletonFactory : 
		IFactory<IUnmarkedSingleton>, 
		IFactory<IUnmarkedSingleton, Type>, 
		IFactory<IMarkedSingleton>, 
		IFactory<IMarkedSingleton, Type> { }

	public sealed class ConcreteSingletonFactory : 
		IFactory<UnmarkedSingleton>, 
		IFactory<UnmarkedSingleton, Type>, 
		IFactory<MarkedSingleton>, 
		IFactory<MarkedSingleton, Type> { }

}
