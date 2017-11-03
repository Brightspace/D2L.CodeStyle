// analyzer: D2L.CodeStyle.Analyzers.ServiceLocator.OldAndBrokenSingletonLocatorAnalyzer

using System;
using D2L.CodeStyle.Analyzers.ServiceLocator;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public static class OldAndBrokenSingletonLocator {
		public static T Get<T>() where T : class {
			return default( T );
		}
	}

	public sealed class SingletonAttribute : Attribute { }
}

namespace SingletonSpecTests {
	[Singleton]
	public interface IMarkedSingleton { }

	public interface INotMarkedSingleton { }

	public sealed class BadClass {

		public BadClass() { }

		public void UsesSingletonLocatorUnmarked() {
			INotMarkedSingleton problem = /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ OldAndBrokenSingletonLocator.Get<INotMarkedSingleton>() /**/;
		}

		public void UsesSingletonLocatorMarked() {
			IMarkedSingleton ok = OldAndBrokenSingletonLocator.Get<IMarkedSingleton>();
		}

		public void UsesOtherMethodOnLocator() {
			string harmless = OldAndBrokenSingletonLocator.ToString();
		}
	}
}
