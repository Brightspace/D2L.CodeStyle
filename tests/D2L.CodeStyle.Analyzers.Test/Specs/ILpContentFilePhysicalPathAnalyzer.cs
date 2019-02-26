// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ContentFilePhysicalPaths.ILpContentFilePhysicalPathAnalyzer

namespace D2L.LP.Files.Domain {
	public interface ILpContentFile {
		string PhysicalPath { get; }
	}

	public interface ILpContentFileNew : ILpContentFile {
	}

	public interface IOtherFile {
		string PhysicalPath { get; }
	}
}

namespace D2L.CodeStyle.Analyzers.Test {

	internal sealed class ExplicitImplementor : D2L.LP.Files.Domain.ILpContentFile {
		public string PhysicalPath {
			get { return ""; }
		}
	}

	internal sealed class ExplicitImplementorNew : D2L.LP.Files.Domain.ILpContentFileNew {
		public string PhysicalPath {
			get { return ""; }
		}
	}

	internal sealed class ImplicitImplementor : D2L.LP.Files.Domain.ILpContentFile {
		string D2L.LP.Files.Domain.ILPContentFile.PhysicalPath {
			get { return ""; }
		}
	}

	internal sealed class ImplicitImplementorNew : D2L.LP.Files.Domain.ILpContentFileNew {
		string D2L.LP.Files.Domain.ILPContentFile.PhysicalPath {
			get { return ""; }
		}
	}

	internal sealed class OtherFile : D2L.LP.Files.Domain.IOtherFile {
		public string PhysicalPath {
			get { return ""; }
		}

		public static void DoSomething( int num, string str, bool flag ) {
		}
	}
}

namespace SpecTests {

	using D2L.LP.Files.Domain;
	using D2L.CodeStyle.Analyzers.Test;

	internal sealed class Usages {

		public string ExplicitImplementorInterfaceUsage() {
			ILpContentFile explicitImplementor = new ExplicitImplementor();
			return /* ContentFilePhysicalPathUsages */ explicitImplementor.PhysicalPath /**/;
		}

		public string ExplicitImplementorNewInterfaceUsage() {
			ILpContentFile explicitImplementor = new ExplicitImplementorNew();
			return /* ContentFilePhysicalPathUsages */ explicitImplementor.PhysicalPath /**/;
		}

		public string ExplicitImplementorImplementationUsage() {
			ExplicitImplementor explicitImplementor = new ExplicitImplementor();
			return /* ContentFilePhysicalPathUsages */ explicitImplementor.PhysicalPath /**/;
		}

		public string ExplicitImplementorNewImplementationUsage() {
			ExplicitImplementorNew explicitImplementor = new ExplicitImplementorNew();
			return /* ContentFilePhysicalPathUsages */ explicitImplementor.PhysicalPath /**/;
		}

		public string ImplicitImplementorInterfaceUsage() {
			ILpContentFile implicitImplementor = new ImplicitImplementor();
			return /* ContentFilePhysicalPathUsages */ implicitImplementor.PhysicalPath /**/;
		}

		public string ImplicitImplementorNewInterfaceUsage() {
			ILpContentFile implicitImplementor = new ImplicitImplementorNew();
			return /* ContentFilePhysicalPathUsages */ implicitImplementor.PhysicalPath /**/;
		}

		public string UsageInMethodCall() {
			ILpContentFile explicitImplementor = new ExplicitImplementor();
			OtherFile.DoSomething( 1, /* ContentFilePhysicalPathUsages */ explicitImplementor.PhysicalPath /**/, true );
		}

		public string UnrelatedUsage() {
			OtherFile instance = new OtherFile();
			return instance.PhysicalPath;
		}
	}
}
