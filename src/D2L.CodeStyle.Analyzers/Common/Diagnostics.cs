using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common {
	public static class Diagnostics {
		public static readonly DiagnosticDescriptor UnsafeStatic = new DiagnosticDescriptor(
			id: "D2L0002",
			title: "Ensure that static field is safe in undifferentiated servers.",
			messageFormat: "The static field or property '{0}' is unsafe because {1}.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Static fields should not have client-specific or mutable data, otherwise they will not be safe in undifferentiated servers."
		);

		public static readonly DiagnosticDescriptor ImmutableClassIsnt = new DiagnosticDescriptor(
			id: "D2L0003",
			title: "Classes marked as immutable should be immutable.",
			messageFormat: "This class is marked immutable, but it is not, because '{0}'. Check that all fields and properties are immutable.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Classes marked as immutable or that implement interfaces marked immutable should be immutable."
		);

		public static readonly DiagnosticDescriptor RpcContextFirstArgument = new DiagnosticDescriptor(
			id: "D2L0004",
			title: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument",
			messageFormat: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument"
		);

		public static readonly DiagnosticDescriptor RpcArgumentSortOrder = new DiagnosticDescriptor(
			id: "D2L0005",
			title: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)",
			messageFormat: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)"
		);

		public static readonly DiagnosticDescriptor UnsafeSingletonField = new DiagnosticDescriptor(
			id: "D2L0006",
			title: "Ensure that a singleton is safe in undifferentiated servers.",
			messageFormat: "",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Singletons should not have client-specific or mutable data, otherwise they will not be safe in undifferentiated servers."
		);

		public static readonly DiagnosticDescriptor UnnecessaryStaticAnnotation = new DiagnosticDescriptor(
			id: "D2L0007",
			title: "Unnecessary static annotations should be removed to keep the code base clean",
			messageFormat: "The {0} annotation is not necessary because {1} is immutable. Please remove this attribute to keep our code base clean.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error, // this may seem extreme but we want to keep the amount of annotated stuff minimal
			isEnabledByDefault:true,
			description: "Unnecessary static annotations should be removed to keep the code base clean"
		);

		public static readonly DiagnosticDescriptor ConflictingStaticAnnotation = new DiagnosticDescriptor(
			id: "D2L0008",
			title: "Statics.Audited and Statics.Unaudited are mutually exclusive",
			messageFormat: "Statics.Audited and Statics.Unaudited are mutually exclusive. Remove at least one of them.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault:true,
			description: "Statics.Audited and Statics.Unaudited are mutually exclusive. Remove at least one of them."
		);

		public static readonly DiagnosticDescriptor OldAndBrokenLocatorIsObsolete = new DiagnosticDescriptor(
			id: "D2L0009",
			title: "OldAndBrokenServiceLocator should be avoided.  Use dependency injection instead.",
			messageFormat: "OldAndBrokenServiceLocator should be avoided.  Use dependency injection instead.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "OldAndBrokenServiceLocator should be avoided.  Use dependency injection instead."
		);

		public static readonly DiagnosticDescriptor NullPassedToNotNullParameter = new DiagnosticDescriptor(
			id: "D2L0010",
			title: "Parameter cannot be passed with a null value.",
			messageFormat: "Parameter \"{0}\" cannot be passed a null value",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The method being called has declared that this parameter cannot receive null, but a null value is being passed."
		);
	}
}
