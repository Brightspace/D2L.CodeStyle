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
			reg.Register<ISingleton>( new ImmutableThing() );
			reg.Register( new ImmutableThing() ); // inferred generic argument of above
			reg.RegisterPlugin<ISingleton>( new ImmutableThing() );
			reg.RegisterPlugin( new ImmutableThing() ); // inferred generic argument of above
			reg.Register<ISingleton, ImmutableThing>( ObjectScope.Singleton );
			reg.RegisterFactory<ISingleton, ImmutableThingFactory>( ObjectScope.Singleton );
			reg.RegisterPluginFactory<ISingleton, ImmutableThingFactory>( ObjectScope.Singleton );
			reg.Register( typeof( ISingleton ), typeof( ImmutableThing ), ObjectScope.Singleton );
			reg.ConfigurePlugins<ImmutableThing>( ObjectScope.Singleton );
			reg.ConfigureOrderedPlugins<ImmutableThing, SomeComparer<ImmutableThing>>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<ImmutableThing>( ObjectScope.Singleton );
			reg.ConfigureInstancePlugins<ImmutableThing, DefaultExtensionPoint<ImmutableThing>>( ObjectScope.Singleton );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<ImmutableThing>, ImmutableThing>( ObjectScope.Singleton );
			reg.RegisterPlugin<DefaultExtensionPoint<ImmutableThing>, IMarkedSingleton, ImmutableThing>( ObjectScope.Singleton );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaImmutableThing, string>( ObjectScope.Singleton );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaImmutableThing, string, string>( ObjectScope.Singleton );
			reg.RegisterSubInterface<ISingleton, IImmutableSubSingleton>( ObjectScope.Singleton );

			// And factory Singletons or singletons where concrete type is not resolved inspect the interface
			reg.Register<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			reg.RegisterPlugin<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			reg.Register<IImmutableSingleton>( null );
			reg.RegisterFactory<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			reg.RegisterPluginFactory<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );

			// Dynamic object factory registrations inspect the concrete object's ctor parameters
			/* UnsafeSingletonRegistration(SpecTests.ISingleton) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMutableThing, string, string>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(SpecTests.ISingleton) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMutableThing, string>( ObjectScope.Singleton ) /**/;

			// Dyanamic object factory registrations that error out inspect IFactory<TDependencyType>
			/* UnsafeSingletonRegistration(D2L.LP.Extensibility.Activation.Domain.IFactory<SpecTests.ICreatedByDynamicFactory>) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor, string>( ObjectScope.Singleton ) /**/;
			/* UnsafeSingletonRegistration(D2L.LP.Extensibility.Activation.Domain.IFactory<SpecTests.ICreatedByDynamicFactory>) */ reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor, string, string>( ObjectScope.Singleton ) /**/;

			// Non-Singletons are not flagged.
			reg.Register( typeof( INotSingleton ), typeof( DoesntMatter ), ObjectScope.WebRequest );
			reg.Register<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
			reg.RegisterPlugin<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
			reg.RegisterFactory<INotSingleton, DoesntMatter>( ObjectScope.Thread );
			reg.RegisterPluginFactory<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
			reg.RegisterParentAwareFactory<INotSingleton, DoesntMatter>();
			reg.ConfigurePlugins<INotSingleton>( ObjectScope.WebRequest );
			reg.ConfigureOrderedPlugins<INotSingleton, SomeComparer<DoesntMatter>>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<INotSingleton>( ObjectScope.WebRequest );
			reg.ConfigureInstancePlugins<INotSingleton, DefaultExtensionPoint<INotSingleton>>( ObjectScope.WebRequest );
			reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<INotSingleton>, DoesntMatter>( ObjectScope.WebRequest );
			reg.RegisterPlugin<DefaultExtensionPoint<INotSingleton>, INotSingleton, DoesntMatter>( ObjectScope.WebRequest );

			// Interfaces marked as singleton cannot have web request registrations.
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.Register<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.Register<ISingleton, DoesntMatter>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.Register( typeof( ISingleton ), typeof( DoesntMatter ), ObjectScope.WebRequest ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterFactory<ISingleton, DoesntMatter>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterPlugin<ISingleton, DoesntMatter>( ObjectScope.Thread ) /**/;
			/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterPluginFactory<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;

			// DynamicObjectFactory registrations at non-Singleton scope are safe,
			// because the implementation is generated, so it will never be marked.
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaImmutableThing, string>( ObjectScope.WebRequest );
			reg.RegisterDynamicObjectFactory<ICreatedByDynamicFactory, ThingThatIsCreatedByDynamicObjectFactoryViaMutableThing, string>( ObjectScope.WebRequest );

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
				Register<INotSingleton>( null );
			}
			public static void Register<TDependencyType>(
				TDependencyType instance
			);
		}
	}

	[Singleton]
	public interface ISingleton { }
	public interface INotSingleton { }

	[Objects.Immutable]
	public interface IImmutableSingleton : ISingleton { }

	public interface IImmutableSubSingleton : IImmutableSingleton { }

	[Objects.Immutable]
	public sealed class ImmutableThing : ISingleton, INotSingleton { }

	public interface ICreatedByDynamicFactory { }

	public class ThingThatIsCreatedByDynamicObjectFactoryViaImmutableThing : ICreatedByDynamicFactory {
		public ThingThatIsCreatedByDynamicObjectFactoryViaImmutableThing(
			string randomArg,
			[Dependency] ImmutableThing injected
		) { }
	}

	public class ThingThatIsCreatedByDynamicObjectFactoryViaMutableThing : ICreatedByDynamicFactory {
		public ThingThatIsCreatedByDynamicObjectFactoryViaMutableThing(
			string randomArg,
			[Dependency] ISingleton injected
		) { }
	}

	public class ThingThatIsSupposedToBeCreatedByDynamicFactoryButDoesntHavePublicConstructor: ISingletonCreatedByDynamicFactory, INotSingletonCreatedByDynamicFactory {
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

	public sealed class ImmutableThingFactory : 
		IFactory<ImmutableThing>, 
		IFactory<ImmutableThing, Type> { }

}
