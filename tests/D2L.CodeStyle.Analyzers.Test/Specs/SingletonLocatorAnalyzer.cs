// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator.SingletonLocatorAnalyzer

using System;
using D2L.CodeStyle.Analyzers.ServiceLocator;
using D2L.LP.Extensibility.Activation.Domain;
using static D2L.LP.Extensibility.Activation.Domain.SingletonLocator;

namespace D2L.LP.Extensibility.Activation.Domain {
	public static class SingletonLocator {
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
			Func<INotMarkedSingleton> problemFunc = /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ SingletonLocator.Get<INotMarkedSingleton> /**/;
			INotMarkedSingleton loadedIndirectly = problemFunc();
		}

		public void UsesSingletonLocatorUnmarked_ViaLazy() {
			Lazy<INotMarkedSingleton> problemLazy = () => /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ SingletonLocator.Get<INotMarkedSingleton> /**/;
			INotMarkedSingleton loadedLazily = problemLazy.Value;
		}

		public void UsesSingletonLocatorUnmarked() {
			INotMarkedSingleton problem = /* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ SingletonLocator.Get<INotMarkedSingleton>() /**/;
		}

		public void UsesSingletonLocatorMarked() {
			IMarkedSingleton ok = SingletonLocator.Get<IMarkedSingleton>();
		}

		public void UsesOtherMethodOnLocator() {
			string harmless = SingletonLocator.ToString();
		}

		public void UnmarkedLocatorInChain() {
			/* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ SingletonLocator.Get<INotMarkedSingleton>() /**/.SomeOtherMethod();
		}

		public void UsingStatic() {
			/* SingletonLocatorMisuse(SingletonSpecTests.INotMarkedSingleton) */ Get<INotMarkedSingleton>() /**/;
		}
	}
}
