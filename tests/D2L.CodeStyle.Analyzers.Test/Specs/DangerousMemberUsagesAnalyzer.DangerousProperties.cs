// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages.DangerousMemberUsagesAnalyzer

using System;

namespace System.Net {

	public static class ServicePointManager {
		public static int DefaultConnectionLimit { get; set; }
	}
}

namespace System.Web {

	public interface IHttpHandler { }

	public sealed class HttpContext {
		public Exception Error { get } => null;
		public IHttpHandler Handler { get; set; }
	}
}

namespace SpecTests {

	using System.Net;
	using System.Web;
	using D2L.CodeStyle.Annotations;

	internal sealed class UnmarkedUsages {

		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ UnmarkedStaticGetUsage(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}

		public void /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ UnmarkedStaticSetUsage(/**/) {
			ServicePointManager.DefaultConnectionLimit = 7;
		}

		public IHttpHandler /* DangerousPropertiesShouldBeAvoided(System.Web.HttpContext.Handler) */ UnmarkedInstanceGetUsage(/**/) {
			HttpContext context = new HttpContext();
			return context.Handler;
		}

		public void /* DangerousPropertiesShouldBeAvoided(System.Web.HttpContext.Handler) */ UnmarkedInstanceSetUsage(/**/) {
			HttpContext context = new HttpContext();
			context.Handler = null;
		}
	}

	internal sealed class AuditedUsages {

		[DangerousPropertyUsage.Audited( typeof( ServicePointManager ), "DefaultConnectionLimit", "John Doe", "1970-01-01", "Rationale" )]
		public int AuditedStaticGetUsage() {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Audited( typeof( ServicePointManager ), "DefaultConnectionLimit", "John Doe", "1970-01-01", "Rationale" )]
		public void AuditedStaticSetUsage() {
			ServicePointManager.DefaultConnectionLimit = 99;
		}

		[DangerousPropertyUsage.Audited( typeof( HttpContext ), "Handler", "John Doe", "1970-01-01", "Rationale" )]
		public IHttpHandler AuditedInstanceGetUsage() {
			HttpContext context = new HttpContext();
			return context.Handler;
		}

		[DangerousPropertyUsage.Audited( typeof( HttpContext ), "Handler", "John Doe", "1970-01-01", "Rationale" )]
		public void AuditedStaticSetUsage() {
			HttpContext context = new HttpContext();
			context.Handler = null;
		}
	}

	internal sealed class UnauditedUsages {

		[DangerousPropertyUsage.Unaudited( typeof( ServicePointManager ), "DefaultConnectionLimit" )]
		public int UnauditedStaticGetUsage() {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Unaudited( typeof( ServicePointManager ), "DefaultConnectionLimit" )]
		public void UnauditedStaticSetUsage() {
			ServicePointManager.DefaultConnectionLimit = 88;
		}

		[DangerousPropertyUsage.Unaudited( typeof( HttpContext ), "Handler" )]
		public IHttpHandler UnauditedInstanceGetUsage() {
			HttpContext context = new HttpContext();
			return context.Handler;
		}

		[DangerousPropertyUsage.Unaudited( typeof( HttpContext ), "Handler" )]
		public void UnauditedStaticSetUsage() {
			HttpContext context = new HttpContext();
			context.Handler = null;
		}
	}

	internal sealed class MismatchedAuditedUsages {

		[DangerousPropertyUsage.Audited( null, "DefaultConnectionLimit" )]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ NullDeclaringType(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Audited( typeof( string ), "DefaultConnectionLimit" )]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ DifferentDeclaringType(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Audited( typeof( ServicePointManager ), null )]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ NullPropertyName(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Audited( typeof( ServicePointManager ), "Wacky" )]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ DifferentPropertyName(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}

		[DangerousPropertyUsage.Audited]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ MissingParameters(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}
	}

	internal sealed class UnrelatedAttributes {

		[ImmutableAttribute]
		public int /* DangerousPropertiesShouldBeAvoided(System.Net.ServicePointManager.DefaultConnectionLimit) */ Method(/**/) {
			return ServicePointManager.DefaultConnectionLimit;
		}
	}
}
