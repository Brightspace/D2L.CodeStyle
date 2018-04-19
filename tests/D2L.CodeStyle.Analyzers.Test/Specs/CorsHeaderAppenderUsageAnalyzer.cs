// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.CorsHeaderAppenderUsageAnalyzer

using D2L.LP.Web.Cors;

namespace D2L.LP.Web.Cors {

	public interface ICorsHeaderAppender { }

	public class CorsHeaderAppender : ICorsHeaderAppender {
		
		public CorsHeaderAppender() { }

		public CorsHeaderAppender( int i ) { }

	}

	public interface ISomeOtherSafeInterface { }

	public class SomeOtherSafeClass : ISomeOtherSafeInterface { }

}

namespace D2L.LP.Web.ContentHandling.Handlers {

	public class ContentHttpHandler { // Whitelisted by necessity

		public ContentHttpHandler(
			ICorsHeaderAppender corsHelper
		) { }

	}

}

namespace D2L.LP.Web.Files.FileViewing { // Whitelisted by necessity

	public class StreamFileViewerResult {

		public StreamFileViewerResult(
			ICorsHeaderAppender corsHelper
		) { }

	}

}

namespace D2L.LP.Web.Files.FileViewing.Default {

	public class StreamFileViewerResultFactory { // Whitelisted by necessity

		public StreamFileViewerResultFactory(
			ICorsHeaderAppender corsHelper
		) { }

	}

}

namespace D2L.CodeStyle.Analyzers.CorsHeaderAppenderUsageAnalyzer.Examples {

	public sealed class BadClass {

		public BadClass(
			/* DangerousUsageOfCorsHeaderAppender */ ICorsHeaderAppender corsHelper /**/
		) { } 

		public void UsesCorsHelper_ManualInstantiation_NoParams() {
			ICorsHeaderAppender corsHelper = /* DangerousUsageOfCorsHeaderAppender */ new CorsHeaderAppender() /**/;
		}

		public void UsesCorsHelper_ManualInstantiation_WithParams() {
			const int i = 10;
			ICorsHeaderAppender corsHelper = /* DangerousUsageOfCorsHeaderAppender */ new CorsHeaderAppender( i ) /**/;
		}

		public void UsesCorsHelper_ManualInstantiation_OnlyInstantiationTriggersDiagnostic() {
			CorsHeaderAppender corsHelper = /* DangerousUsageOfCorsHeaderAppender */ new CorsHeaderAppender() /**/;
		}
	}

	public sealed class GoodClass {

		ISomeOtherSafeInterface m_safeInterface;

		public GoodClass(
			ISomeOtherSafeInterface safeInterface
		) {
			m_safeInterface = safeInterface;
		}

		public void SafeMethod() {
			ISomeOtherSafeInterface safeInterface = new SomeOtherSafeClass();
		}

	}

}
