// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.RpcAnalyzer

using System;
using D2L.LP.Extensibility.Activation.Domain;
using D2L.Web;
using D2L.Web.RequestContext;

namespace D2L.Web {
	public interface IRpcContext { }
	public interface IRpcPostContext { }
	public class RpcAttribute : Attribute { }
}

namespace D2L.Web.RequestContext {
	public interface IRpcPostContextBase { }
}

namespace D2L.LP.Extensibility.Activation.Domain {
	public class DependencyAttribute : Attribute { }
}

namespace D2L.CodeStyle.Analyzers.RpcDependencies.Examples {
	public sealed class FooDependency { }
	public sealed class BarDependency { }

	public sealed class OkayRpcHandler {
		public void NonRpcMethod( int x ) { }

		public static void NonRpcStaticMethod() { }

		[UndefinedAttribute]
		public static void WeShouldntEmitDiagnosticsWhenAnAttributeDoesntExist() { }

		[Rpc]
		public static void BasicIRpcContext( IRpcContext context ) { }

		[Rpc]
		public static void BasicIRpcPostContext( IRpcPostContext context ) { }

		[Rpc]
		public static void BasicIRpcPostContextBase( IRpcPostContextBase context ) { }

		[Rpc]
		public static void RpcWithSingleParameter( IRpcContext context, int x ) { }

		[Rpc]
		public static void RpcWithMultipleParameters( IRpcContext context, int x, int y ) { }
		
		[Rpc]
		public static void RpcWithDependencyParameter(
			IRpcContext context,
			[Dependency] FooDependency x
		) { }

		[Rpc]
		public static void RpcWithMultipleDependencyParameters(
			IRpcContext context,
			[Dependency] FooDependency x,
			[Dependency] BarDependency y
		) { }

		[Rpc]
		public static void GeneralRpc(
			IRpcContext context,
			[Dependency] FooDependency x,
			[Dependency] BarDependency y,
			int a,
			string b
		) { }

		[Rpc]
		public static void GeneralRpcWithWeirdParameterAttribute(
			IRpcContext context,
			[Dependency] FooDependency x,
			[Dependency] BarDependency y,
			int a,
			[UndefinedAttribute] string b
		) { }
	}

	public sealed class BadRpcs {
		// The first argument must be wither IRpcContext, IRpcPostContext or IRpcPostContextBase

		[Rpc]
		public static void MissingFirstArgument /* RpcContextFirstArgument */ () /**/ { }

		[Rpc]
		public static void BadFirstArgument( /* RpcContextFirstArgument */ int x /**/ ) { }

		[Rpc]
		[UndefinedAttribute]
		public static void BadFirstArgumentWithUnrelatedUndefinedMethodAttribute( /* RpcContextFirstArgument */ int x /**/ ) { }

		[Rpc]
		public static void BadFirstArgument2(
			/* RpcContextFirstArgument */ int x /**/,
			IRpcContext context
		) { }

		// [Dependency] arguments must come after the first argument but before
		// any of the arguments that aren't [Dependency] (those come from the
		// user-agent.)

		[Rpc]
		public static void IncorrectDependencySortOrder(
			IRpcContext context,
			int x,
			/* RpcArgumentSortOrder */ [Dependency] FooDependency foo /**/
		) { }

		[Rpc]
		public static void IncorrectDependencySortOrderWithMultipleThings(
			IRpcContext context,
			int x,
			int y,
			/* RpcArgumentSortOrder */ [Dependency] FooDependency foo /**/,
			/* RpcArgumentSortOrder */ [Dependency] BarDependency bar /**/
		) { }

		[Rpc]
		public static void IncorrectDependencySortOrderEvenWithRandomBadAttributeInTheMiddle(
			IRpcContext context,
			[UndefinedAttribute] int x,
			/* RpcArgumentSortOrder */ [Dependency] FooDependency foo /**/
		) { }
	}
}
