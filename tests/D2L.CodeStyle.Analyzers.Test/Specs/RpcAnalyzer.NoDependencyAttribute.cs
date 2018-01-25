// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.RpcAnalyzer

using System;
namespace D2L.Web {
	public interface IRpcContext { }
	public interface IRpcPostContext { }
	public class RpcAttribute : Attribute { }
}

namespace D2L.Web.RequestContext {
	public interface IRpcPostContextBase { }
}

namespace D2L {
	class WeirdAttribute : Attribute { }
	class Examples {
		// This shouldn't crash even though DependencyAttribute isn't defined
		[D2L.Web.Rpc]
		public static void BasicRpc(
			D2L.Web.IRpcContext context,
			[Weird] int x
		) { }
	}
}
