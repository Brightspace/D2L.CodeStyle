// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator.OldAndBrokenServiceLocatorAnalyzer

using System;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {

	public sealed class DIFrameworkAttribute : Attribute { }

	[DIFramework]
	public interface ICustomObjectActivator : IObjectActivator, IDisposable { }

	public interface IObjectActivator {
		T Create<T>();
	}

	public interface IServiceLocator { }

	public static class OldAndBrokenServiceLocator {
		public static IServiceLocator Instance {
			get { return null; }
		}
	}

	public class OldAndBrokenServiceLocatorFactory {
		public static IServiceLocator Create() { return null; }
	}

	[DIFramework]
	public static class ObjectActivatorExtensions {
		public static bool TryCreateInstance<T, TF>(
			this IObjectActivator activator,
			out T instance
		) {
			instance = null;
			return false;
		}
	}
}

namespace D2L.LP.Extensibility.Activation.Domain.Default.StaticDI {

	internal interface ISimpleActivator { }

	public static class StaticDILocator {
		internal static ISimpleActivator Current { get; }
		internal static ISimpleActivator CreateIsolatedActivator() { return default; }
	}
}

namespace D2L.CodeStyle.Analyzers.OldAndBrokenLocator.Examples {
	using D2L.LP.Extensibility.Activation.Domain.Default.StaticDI;

	public sealed class FooDependency { }
	public sealed class BarDependency { }

	public sealed class BadClass {

		private readonly IObjectActivator /* OldAndBrokenLocatorIsObsolete */ m_objectActivator /**/;

		public BadClass( IObjectActivator /* OldAndBrokenLocatorIsObsolete */ objectActivator /**/ ) {
			m_objectActivator = objectActivator;
		}

		public IObjectActivator /* OldAndBrokenLocatorIsObsolete */ Activator /**/ {
			get { return m_objectActivator; }
		}

		public void Uses_OldAndBrokenServiceLocator() {
			IServiceLocator locator = /* OldAndBrokenLocatorIsObsolete */ OldAndBrokenServiceLocator.Instance /**/;
		}

		public void Uses_OldAndBrokenServiceLocatorFactory() {
			IServiceLocator locator = /* OldAndBrokenLocatorIsObsolete */ OldAndBrokenServiceLocatorFactory.Create() /**/;
			Func<IServiceLocator> locatorFactory =  /* OldAndBrokenLocatorIsObsolete */ OldAndBrokenServiceLocatorFactory.Create /**/;
		}

		public void Uses_IObjectActivator() {
			IObjectActivator activator = default;
			/* OldAndBrokenLocatorIsObsolete */ activator.Create<string>() /**/;
			Func<string> activatorFunc = /* OldAndBrokenLocatorIsObsolete */ activator.Create<string> /**/;
		}

		public void Uses_IObjectActivatorExtension() {
			IObjectActivator activator = default;
			/* OldAndBrokenLocatorIsObsolete */ activator.TryCreateInstance<string, string>( out string instance ) /**/;
			var x = /* OldAndBrokenLocatorIsObsolete */ activator.TryCreateInstance<string, string> /**/;
		}

		public void Uses_ICustomObjectActivator() {
			ICustomObjectActivator activator = default;
			/* OldAndBrokenLocatorIsObsolete */ activator.Create<string>() /**/;
			Func<string> activatorFunc = /* OldAndBrokenLocatorIsObsolete */ activator.Create<string> /**/;
		}

		public void Uses_StaticDILocator() {
			ISimpleActivator activator = /* OldAndBrokenLocatorIsObsolete */ StaticDILocator.Current /**/;
			ISimpleActivator activator = /* OldAndBrokenLocatorIsObsolete */ StaticDILocator.CreateIsolatedActivator() /**/;
		}
	}

	public sealed class SketchyButNotYetOutlawedInjection {
		IServiceLocator m_locator;

		public SketchyButNotYetOutlawedInjection(
			IServiceLocator injectedLocator
		) {
			m_locator = injectedLocator;
		}
	}

	public sealed class GoodInjection {
		FooDependency m_foo;
		BarDependency m_bar;

		public GoodInjection(
			FooDependency injectedFoo,
			BarDependency injectedBar
		) {
			m_foo = injectedFoo;
			m_bar = injectedBar;
		}
	}

	[DIFramework]
	public sealed class DIFrameworkClass {

		public int InDIFrameworkClass() {
			IServiceLocator ok = OldAndBrokenServiceLocator.Instance;
			IServiceLocator alsoOk = OldAndBrokenServiceLocatorFactory.Create();
			return 0;
		}
	}

	[DIFramework]
	public sealed class DIFrameworkUsageInNestedClass {
		private static class Nested {
			public static void Usage() {
				IServiceLocator ok = OldAndBrokenServiceLocator.Instance;
				IServiceLocator alsoOk = OldAndBrokenServiceLocatorFactory.Create();
			}
		}
	}
}
