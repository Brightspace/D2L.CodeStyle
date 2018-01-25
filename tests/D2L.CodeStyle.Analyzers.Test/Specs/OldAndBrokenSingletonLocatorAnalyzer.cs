// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator.OldAndBrokenSingletonLocatorAnalyzer

using System;
using D2L.CodeStyle.Analyzers.ServiceLocator;
using D2L.LP.Extensibility.Activation.Domain;
using static D2L.LP.Extensibility.Activation.Domain.OldAndBrokenSingletonLocator;

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

	public interface INotMarkedSingleton {
		internal void SomeOtherMethod() { }
	}

	public sealed class BadClass {

		public BadClass() { }

		public void UsesSingletonLocatorUnmarked_ViaFunc() {
			Func<INotMarkedSingleton> problemFunc = /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ OldAndBrokenSingletonLocator.Get<INotMarkedSingleton> /**/;
			INotMarkedSingleton loadedIndirectly = problemFunc();
		}

		public void UsesSingletonLocatorUnmarked_ViaLazy() {
			Lazy<INotMarkedSingleton> problemLazy = () => /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ OldAndBrokenSingletonLocator.Get<INotMarkedSingleton> /**/;
			INotMarkedSingleton loadedLazily = problemLazy.Value;
		}

		public void UsesSingletonLocatorUnmarked() {
			INotMarkedSingleton problem = /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ OldAndBrokenSingletonLocator.Get<INotMarkedSingleton>() /**/;
		}

		public void UsesSingletonLocatorMarked() {
			IMarkedSingleton ok = OldAndBrokenSingletonLocator.Get<IMarkedSingleton>();
		}

		public void UsesOtherMethodOnLocator() {
			string harmless = OldAndBrokenSingletonLocator.ToString();
		}

		public void UnmarkedLocatorInChain() {
			/* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ OldAndBrokenSingletonLocator.Get<INotMarkedSingleton>() /**/.SomeOtherMethod();
		}

		public void UsingStatic() {
			/* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ Get<INotMarkedSingleton>() /**/;
		}
	}
}
