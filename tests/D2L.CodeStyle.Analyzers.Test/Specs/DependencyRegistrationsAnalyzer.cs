// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.DependencyRegistrationsAnalyzer

using System;
using System.Collections.Generic;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

// copied from: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/IDependencyRegistry.cs
// and: http://search.dev.d2l/source/raw/Lms/core/lp/framework/core/D2L.LP.Foundation/LP/Extensibility/Activation/Domain/ObjectScope.cs
namespace D2L.LP.Extensibility.Activation.Domain {
	public sealed class SingletonAttribute : Attribute { }
	public sealed class DependencyAttribute : Attribute { }
	public enum ObjectScope {
		Singleton = 1,
		WebRequest = 3
	}
	public abstract class ExtensionPointDescriptor {
		public abstract string Name { get; }
	}

	public interface IExtensionPoint<T> { }
	public interface IFactory<out TDependencyType> { }
	public interface IFactory<out TDependencyType, T> { }
	public interface IDependencyRegistry {

		void Register<TDependencyType, TConcreteType>(
				ObjectScope scope
			) where TConcreteType : TDependencyType;

		void Register( Type dependencyType, Type concreteType, ObjectScope scope );

		void RegisterFactory<TDependencyType, TFactoryType>(
				ObjectScope scope
			) where TFactoryType : IFactory<TDependencyType>;

		void RegisterPlugin<TDependencyType, TConcreteType>(
				ObjectScope scope
			) where TConcreteType : TDependencyType;

		void RegisterPluginFactory<TDependencyType, TFactoryType>(
				ObjectScope scope
			) where TFactoryType : IFactory<TDependencyType>;

		void ConfigurePlugins<TPlugin>(
			ObjectScope scope
		);

		void ConfigureOrderedPlugins<TPlugin, TComparer>(
			ObjectScope scope
		) where TComparer : IComparer<TPlugin>, new();

		void ConfigurePlugins<TExtensionPoint, T>(
			ObjectScope scope
		) where TExtensionPoint : IExtensionPoint<T>;

		void ConfigureOrderedPlugins<TExtensionPoint, TPlugin, TComparer>(
			ObjectScope scope
		)
			where TExtensionPoint : IExtensionPoint<T>
			where TComparer : IComparer<TPlugin>, new()
		;

		void RegisterPluginExtensionPoint<TExtensionPoint, T>(
				ObjectScope scope
			) where TExtensionPoint : IExtensionPoint<T>;

		void RegisterPlugin<TExtensionPoint, TDependencyType, TConcreteType>(
				ObjectScope scope
			)
			where TConcreteType : TDependencyType
			where TExtensionPoint : IExtensionPoint<TDependencyType>;

		void RegisterPluginFactory<TExtensionPoint, TDependencyType, TFactoryType>(
				ObjectScope scope
			)
			where TFactoryType : IFactory<TDependencyType>
			where TExtensionPoint : IExtensionPoint<TDependencyType>;

		void UnhandledRegisterMethod();
	}
	public static class ExtensionMethods {
		// from: http://search.dev.d2l/source/xref/Lms/core/lp/framework/core/D2L.LP/LP/Extensibility/Plugins/DI/LegacyPluginsDependencyLoaderExtensions.cs
		public static void ConfigureInstancePlugins<TPlugin>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) {
		}

		public static void ConfigureInstancePlugins<TPlugin, TExtensionPoint>(
				this IDependencyRegistry registry,
				ObjectScope scope
			) where TExtensionPoint : ExtensionPointDescriptor, new() {
		}

		public static void RegisterSubInterface<TSubInterfaceType, TInjectableSuperInterfaceType>(
				this IDependencyRegistry @this,
				ObjectScope scope
			) where TInjectableSuperInterfaceType : TSubInterfaceType {
		}
	}
}

namespace SpecTests {
	public sealed class SomeTestCases {
		public void DoesntMatter( IDependencyRegistry reg ) {

			//// Marked Singletons are not flagged.
			//reg.Register<ISingleton, ImmutableThing>( ObjectScope.Singleton );
			//reg.RegisterFactory<ISingleton, ImmutableThingFactory>( ObjectScope.Singleton );
			//reg.RegisterPluginFactory<ISingleton, ImmutableThingFactory>( ObjectScope.Singleton );
			//reg.Register( typeof( ISingleton ), typeof( ImmutableThing ), ObjectScope.Singleton );
			reg.ConfigurePlugins<ImmutableThing>( ObjectScope.Singleton );
			reg.ConfigureOrderedPlugins<ImmutableThing, SomeComparer<ImmutableThing>>( ObjectScope.Singleton );
			//reg.ConfigureInstancePlugins<ImmutableThing>( ObjectScope.Singleton );
			//reg.ConfigureInstancePlugins<ImmutableThing, DefaultExtensionPoint<ImmutableThing>>( ObjectScope.Singleton );
			//reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<ImmutableThing>, ImmutableThing>( ObjectScope.Singleton );
			//reg.RegisterPlugin<DefaultExtensionPoint<ISingleton>, ISingleton, ImmutableThing>( ObjectScope.Singleton );
			//reg.RegisterSubInterface<ISingleton, IImmutableSubSingleton>( ObjectScope.Singleton );

			//// And factory Singletons or singletons where concrete type is not resolved inspect the interface
			//reg.Register<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			//reg.RegisterPlugin<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			//reg.RegisterFactory<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			//reg.RegisterPluginFactory<IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );
			//reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, NonExistentTypeOrInTheMiddleOfTyping>( ObjectScope.Singleton );


		//	// Non-Singletons are not flagged.
		//	reg.Register( typeof( INotSingleton ), typeof( DoesntMatter ), ObjectScope.WebRequest );
		//	reg.Register<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
		//	reg.RegisterPlugin<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
		//	reg.RegisterFactory<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
		//	reg.RegisterPluginFactory<INotSingleton, DoesntMatter>( ObjectScope.WebRequest );
		//	reg.ConfigurePlugins<INotSingleton>( ObjectScope.WebRequest );
		//	reg.ConfigureOrderedPlugins<INotSingleton, SomeComparer<DoesntMatter>>( ObjectScope.WebRequest );
		//	reg.ConfigureInstancePlugins<INotSingleton>( ObjectScope.WebRequest );
		//	reg.ConfigureInstancePlugins<INotSingleton, DefaultExtensionPoint<INotSingleton>>( ObjectScope.WebRequest );
		//	reg.RegisterPluginExtensionPoint<DefaultExtensionPoint<INotSingleton>, DoesntMatter>( ObjectScope.WebRequest );
		//	reg.RegisterPlugin<DefaultExtensionPoint<INotSingleton>, INotSingleton, DoesntMatter>( ObjectScope.WebRequest );

		//	// Interfaces marked as singleton cannot have web request registrations.
		//	/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.Register<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;
		//	/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.Register( typeof( ISingleton ), typeof( DoesntMatter ), ObjectScope.WebRequest ) /**/;
		//	/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterFactory<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;
		//	/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterPlugin<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;
		//	/* AttributeRegistrationMismatch(SpecTests.ISingleton) */ reg.RegisterPluginFactory<ISingleton, DoesntMatter>( ObjectScope.WebRequest ) /**/;

		//	// Unhandled registration methods should raise a diagnostic.
		//	/* RegistrationKindUnknown */ reg.UnhandledRegisterMethod() /**/;

		//	// Concrete types should have a public constructor
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithInternalConstructor) */ reg.Register<IImmutableSingleton, ThingWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithPrivateConstructor) */ reg.Register<IImmutableSingleton, ThingWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	reg.Register<IImmutableSingleton, ThingWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.Register<IImmutableSingleton, ThingWithPrivateAndPublicConstructors>( ObjectScope.Singleton );
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithInternalConstructor) */ reg.RegisterFactory<IImmutableSingleton, ThingFactoryWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithPrivateConstructor) */ reg.RegisterFactory<IImmutableSingleton, ThingFactoryWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	reg.RegisterFactory<IImmutableSingleton, ThingFactoryWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterFactory<IImmutableSingleton, ThingFactoryWithPrivateAndPublicConstructors>( ObjectScope.Singleton );
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithInternalConstructor) */ reg.RegisterPlugin<IImmutableSingleton, ThingWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithPrivateConstructor) */ reg.RegisterPlugin<IImmutableSingleton, ThingWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithInternalConstructor) */ reg.RegisterPlugin<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithPrivateConstructor) */ reg.RegisterPlugin<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	reg.RegisterPlugin<IImmutableSingleton, ThingWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPlugin<IImmutableSingleton, ThingWithPrivateAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPlugin<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPlugin<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingWithPrivateAndPublicConstructors>( ObjectScope.Singleton );

		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithInternalConstructor) */ reg.RegisterPluginFactory<IImmutableSingleton, ThingFactoryWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithPrivateConstructor) */ reg.RegisterPluginFactory<IImmutableSingleton, ThingFactoryWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithInternalConstructor) */ reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingFactoryWithInternalConstructor>( ObjectScope.Singleton ) /**/;
		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingFactoryWithPrivateConstructor) */ reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingFactoryWithPrivateConstructor>( ObjectScope.Singleton ) /**/;
		//	reg.RegisterPluginFactory<IImmutableSingleton, ThingFactoryWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPluginFactory<IImmutableSingleton, ThingFactoryWithPrivateAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingFactoryWithInternalAndPublicConstructors>( ObjectScope.Singleton );
		//	reg.RegisterPluginFactory<DefaultExtensionPoint<IImmutableSingleton>, IImmutableSingleton, ThingFactoryWithPrivateAndPublicConstructors>( ObjectScope.Singleton );

		//	/* DependencyRegistraionMissingPublicConstructor(SpecTests.ThingWithInternalConstructor) */ reg.Register( typeof( IImmutableSingleton ), typeof( ThingWithInternalConstructor ), ObjectScope.Singleton ) /**/;
		//}

		//// Registrations in some classes/structs are ignored because they 
		//// are wrappers of other register methods and we don't have enough
		//// to analyze.
		//public static class RegistrationCallsInThisClassAreIgnored {
		//	public static void DoesntMatter<T>( IDependencyRegistry reg ) where T : new() {
		//		reg.Register<T>( new T() );
		//	}
		//}
		//public struct RegistrationCallsInThisStructAreIgnored {
		//	public static void DoesntMatter<T>( IDependencyRegistry reg ) where T : new() {
		//		reg.Register<T>( new T() );
		//	}
		//}

		//public static class OnlyRegistrationMethodsOnIDependencyRegistryMatter {
		//	public static void DoesntMatter( IDependencyRegistry reg ) {
		//		Register<INotSingleton>( null );
		//	}

		//	public static void Register<TDependencyType>(
		//		TDependencyType instance
		//	) {
		//	}
		}
	}

	[Singleton]
	public interface ISingleton { }
	public interface INotSingleton { }

	[Objects.Immutable]
	public interface IImmutableSingleton : ISingleton { }

	[Objects.Immutable]
	public interface IImmutableSubSingleton : IImmutableSingleton { }

	[Objects.Immutable]
	public sealed class ImmutableThing : ISingleton, INotSingleton { }

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

	[Objects.Immutable]
	internal sealed class ThingWithInternalConstructor : IImmutableSingleton {
		internal ThingWithInternalConstructor() { }
	}

	[Objects.Immutable]
	internal sealed class ThingWithPrivateConstructor : IImmutableSingleton {
		private ThingWithPrivateConstructor() { }
	}

	[Objects.Immutable]
	internal sealed class ThingWithInternalAndPublicConstructors : IImmutableSingleton {
		internal ThingWithInternalAndPublicConstructors() { }
		public ThingWithInternalAndPublicConstructors() { }
	}

	[Objects.Immutable]
	internal sealed class ThingWithPrivateAndPublicConstructors : IImmutableSingleton {
		private ThingWithPrivateAndPublicConstructors() { }
		public ThingWithPrivateAndPublicConstructors() { }
	}

	internal sealed class ThingFactoryWithInternalConstructor : IFactory<IImmutableSingleton> {
		internal ThingFactoryWithInternalConstructor() { }
	}

	internal sealed class ThingFactoryWithPrivateConstructor : IFactory<IImmutableSingleton> {
		private ThingFactoryWithPrivateConstructor() { }
	}

	internal sealed class ThingFactoryWithInternalAndPublicConstructors : IFactory<IImmutableSingleton> {
		internal ThingFactoryWithInternalAndPublicConstructors() { }
		public ThingFactoryWithInternalAndPublicConstructors() { }
	}

	internal sealed class ThingFactoryWithPrivateAndPublicConstructors : IFactory<IImmutableSingleton> {
		internal ThingFactoryWithPrivateAndPublicConstructors() { }
		public ThingFactoryWithPrivateAndPublicConstructors() { }
	}

}
