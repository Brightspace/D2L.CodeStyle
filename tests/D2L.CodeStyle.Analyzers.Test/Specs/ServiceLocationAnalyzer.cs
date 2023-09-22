// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator.ServiceLocationAnalyzer

using System;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.LP.Extensibility.Activation.Domain {
	public static class SingletonLocator {
		public static T Get<T>() where T : class {
			return default( T );
		}
	}

	public static interface IServiceLocator {
		bool TryGet<T>( out T service );
		bool TryGet( Type type, out object service );
		T Get<T>();
		object Get( Type type );
	}

	public sealed class UnlocatableAttribute : Attribute { }
	public static class Unlocatable {
		public sealed class CandidateAttribute : Attribute { }
	}

	public interface IPlugins<out T> : System.Collections.Generic.IEnumerable<T> { }
	public interface IPlugins<TExtensionPoint, out T> : System.Collections.Generic.IEnumerable<T> { }

	public interface IPlugins<TExtensionPoint, TOther, out T> : System.Collections.Generic.IEnumerable<T> { }
}

namespace TT {
	public interface IInterface { }

	public interface ISomeExtensionPoint { }

	[Unlocatable]
	public interface IUnlocatable { }

	[Unlocatable.Candidate]
	public interface IUnlocatableCandidate { }
}

namespace ServiceLocationAnalyzerSpecTests {
	using TT;

	public sealed class GoodClass {

		private readonly IServiceLocator serviceLocator;

		public void Tests_SingletonLocator() {
			SingletonLocator.Get<IInterface>();
			SingletonLocator.Get<IPlugins<IInterface>>();
			SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IInterface>>();

			var f1 = SingletonLocator.Get<IInterface>;
			var f2 = SingletonLocator.Get<IPlugins<IInterface>>;
			var f3 = SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IInterface>>;
		}

		public void Tests_ServiceLocator_Get() {
			serviceLocator.Get<IInterface>();
			serviceLocator.Get<IPlugins<IInterface>>();
			serviceLocator.Get<IPlugins<ISomeExtensionPoint, IInterface>>();

			var f1 = serviceLocator.Get<IInterface>;
			var f2 = serviceLocator.Get<IPlugins<IInterface>>;
			var f3 = serviceLocator.Get<IPlugins<ISomeExtensionPoint, IInterface>>;
		}

		public void Tests_ServiceLocator_TryGet() {
			serviceLocator.TryGet<IInterface>( out var s1 );
			serviceLocator.TryGet<IPlugins<IInterface>>( out var s2 );
			serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IInterface>>( out var s3 );

			var f1 = serviceLocator.TryGet<IInterface>;
			var f2 = serviceLocator.TryGet<IPlugins<IInterface>>;
			var f3 = serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IInterface>>;
		}

	}

	public sealed class BadClass_Unlocatable {

		private readonly IServiceLocator serviceLocator;

		public void Tests_SingletonLocator() {
			/* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IUnlocatable>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IPlugins<IUnlocatable>>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatable>>() /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IUnlocatable> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IPlugins<IUnlocatable>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatable) */ SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatable>> /**/;
		}

		public void Tests_ServiceLocator_Get() {
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IUnlocatable>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IPlugins<IUnlocatable>>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatable>>() /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IUnlocatable> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IPlugins<IUnlocatable>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatable>> /**/;
		}

		public void Tests_ServiceLocator_TryGet() {
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IUnlocatable>( out var s1 ) /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IPlugins<IUnlocatable>>( out var s2 ) /**/;
			/* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IUnlocatable>>( out var s3 ) /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IUnlocatable> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IPlugins<IUnlocatable>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatable) */ serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IUnlocatable>> /**/;
		}

	}

	public sealed class BadClass_UnlocatableCandidate {

		private readonly IServiceLocator serviceLocator;

		public void Tests_SingletonLocator() {
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IUnlocatableCandidate>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IPlugins<IUnlocatableCandidate>>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>>() /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IUnlocatableCandidate> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IPlugins<IUnlocatableCandidate>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ SingletonLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>> /**/;
		}

		public void Tests_ServiceLocator_Get() {
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IUnlocatableCandidate>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IPlugins<IUnlocatableCandidate>>() /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>>() /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IUnlocatableCandidate> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IPlugins<IUnlocatableCandidate>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.Get<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>> /**/;
		}

		public void Tests_ServiceLocator_TryGet() {
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IUnlocatableCandidate>( out var s1 ) /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IPlugins<IUnlocatableCandidate>>( out var s2 ) /**/;
			/* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>>( out var s3 ) /**/;

			var f1 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IUnlocatableCandidate> /**/;
			var f2 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IPlugins<IUnlocatableCandidate>> /**/;
			var f3 = /* LocatedUnlocatable(TT.IUnlocatableCandidate) */ serviceLocator.TryGet<IPlugins<ISomeExtensionPoint, IUnlocatableCandidate>> /**/;
		}

	}
}
