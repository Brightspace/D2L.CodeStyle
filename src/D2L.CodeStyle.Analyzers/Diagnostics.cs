﻿using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {
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

		public static readonly DiagnosticDescriptor UnsafeSingletonRegistration = new DiagnosticDescriptor(
			id: "D2L0006",
			title: "Ensure that a singleton is safe in undifferentiated servers.",
			messageFormat: "The type '{0}' is not safe to register as a singleton, because it is not marked with [Immutable].",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Singletons should be marked with the [Immutable] attribute."
		);

		public static readonly DiagnosticDescriptor UnnecessaryStaticAnnotation = new DiagnosticDescriptor(
			id: "D2L0007",
			title: "Unnecessary static annotations should be removed to keep the code base clean",
			messageFormat: "The {0} annotation is not necessary because {1} is immutable. Please remove this attribute to keep our code base clean.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Unnecessary static annotations should be removed to keep the code base clean"
		);

		public static readonly DiagnosticDescriptor ConflictingStaticAnnotation = new DiagnosticDescriptor(
			id: "D2L0008",
			title: "Statics.Audited and Statics.Unaudited are mutually exclusive",
			messageFormat: "Statics.Audited and Statics.Unaudited are mutually exclusive. Remove at least one of them.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
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

		public static readonly DiagnosticDescriptor SingletonRegistrationTypeUnknown = new DiagnosticDescriptor(
			id: "D2L0011",
			title: "Unable to resolve the concrete or plugin type for this registration.",
			messageFormat: "Unable to determine the concrete or plugin type for this registration; please make sure to reference the type's assembly.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Singleton registrations must be known at compile-time; please make sure to reference the type's assembly."
		);

		public static readonly DiagnosticDescriptor RegistrationKindUnknown = new DiagnosticDescriptor(
			id: "D2L0012",
			title: "Unable to determine the kind of dependency registration attempted.",
			messageFormat: "The attempted DI registration is not known to our analysis or there was an error analyzing it. This is mostly likely because the ObjectScope is being passed as a variable, or this is a new kind of registration and needs to be handled.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "All DI registrations must be known to static analyzers, to allow for thorough analysis."
		);

		public static readonly DiagnosticDescriptor ClassShouldBeSealed = new DiagnosticDescriptor(
			id: "D2L0013",
			title: "Non-public class should be sealed because it doesn't have any subtypes.",
			messageFormat: "Non-public class should be sealed because it doesn't have any subtypes.",
			category: "Style",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Non-public class should be sealed because it doesn't have any subtypes."
		);

		public static readonly DiagnosticDescriptor SingletonIsntImmutable = new DiagnosticDescriptor(
			id: "D2L0014",
			title: "Classes marked as a singleton should be immutable.",
			messageFormat: "This class is marked as a singleton, but it is not marked immutable.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Classes marked as singleton or that implement interfaces marked as a singleton should be marked immutable."
		);

		public static readonly DiagnosticDescriptor SingletonLocatorMisuse = new DiagnosticDescriptor(
			id: "D2L0017",
			title: "Can only use OldAndBrokenSingletonLocator to inject interfaces with the [Singleton] attribute",
			messageFormat: "Cannot use OldAndBrokenSingletonLocator to inject {0} because it lacks the [Singleton] attribute",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Can only use OldAndBrokenSingletonLocator to inject interfaces with the [Singleton] attribute"
		);

		public static readonly DiagnosticDescriptor DangerousMethodsShouldBeAvoided = new DiagnosticDescriptor(
			id: "D2L0018",
			title: "Avoid using dangerous methods",
			messageFormat: "Should not use {0} because it's considered dangerous",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Avoid using of dangerous methods"
		);

		public static readonly DiagnosticDescriptor AttributeRegistrationMismatch = new DiagnosticDescriptor(
			id: "D2L0019",
			title: "Singleton attribute cannot appear on non-singleton object scopes.",
			messageFormat: "The type '{0}' is marked as [Singleton] but is registered with a conflicting object scope.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Non-Singletons should not be marked with the [Singleton] attribute."
		);

		public static readonly DiagnosticDescriptor InvalidLaunchDarklyFeatureDefinition = new DiagnosticDescriptor(
			id: "D2L0021",
			title: "Launch Darkly feature definitions are limited to support types",
			messageFormat: "Invalid feature flag value type: {0}",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Must be one of: bool, int, float, string"
		);

		public static readonly DiagnosticDescriptor ObsoleteILaunchDarklyClientClient = new DiagnosticDescriptor(
			id: "D2L0022",
			title: "Use the new IInstanceFlag/IOrgFlag/ICurrentOrgFlag interfaces instead",
			messageFormat: "Should not use D2L.LP.LaunchDarkly.ILaunchDarklyClient because it's obsolete",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Switch your feature definition to inherit D2L.LP.LaunchDarkly.FeatureDefinition, configure your feature for DI, and use one of the new IInstanceFlag/IOrgFlag/ICurrentOrgFlag interfaces instead"
		);

		public static readonly DiagnosticDescriptor InvalidUnauditedReasonInImmutable = new DiagnosticDescriptor(
			id: "D2L0023",
			title: "Immutability exceptions must be a subset of containing type's",
			messageFormat: "One or more members on this type have unaudited reasons that are not excepted. Resolve the Unaudited members or add {{ {0} }} to the type's immutable exceptions.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Type's members' allowed and used unaudited reasons must be a subset of the type's immutable exceptions."
		);

		public static readonly DiagnosticDescriptor ImmutableExceptionInheritanceIsInvalid = new DiagnosticDescriptor(
			id: "D2L0024",
			title: "Immutable exceptions are not valid for this type.",
			messageFormat: "This type is marked immutable, but it has more permissive immutability than its base type '{0}'. {1}",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Immutable exceptions must be a subset of all parent types' exceptions."
		);

		public static readonly DiagnosticDescriptor ObsoleteJsonParamBinder = new DiagnosticDescriptor(
			id: "D2L0025",
			title: "Use the new JsonConvertParameterBinder instead",
			messageFormat: "Should not use JsonParamBinder because it is obsolete",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "JsonParamBinder uses the custom D2L JSON framework, so use JsonConvertParameterBinder (which uses Newtonsoft.Json) instead."
		);

		public static readonly DiagnosticDescriptor ImmutableGenericAttributeInWrongAssembly = new DiagnosticDescriptor(
			id: "D2L0026",
			title: "Cannot apply ImmutableGeneric for the given type.",
			messageFormat: "Cannot apply ImmutableGeneric for the type '{0}'.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "ImmutableGeneric can only be applied in an assembly where the generic type definition or one of its type arguments is declared."
		);

		public static readonly DiagnosticDescriptor ImmutableGenericAttributeAppliedToNonGenericType = new DiagnosticDescriptor(
			id: "D2L0027",
			title: "Cannot apply ImmutableGeneric for a non-generic type.",
			messageFormat: "Cannot apply ImmutableGeneric for the non-generic type '{0}'.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "ImmutableGeneric can only be applied to generic types."
		);

		public static readonly DiagnosticDescriptor ImmutableGenericAttributeAppliedToOpenGenericType = new DiagnosticDescriptor(
			id: "D2L0028",
			title: "Cannot apply ImmutableGeneric for an open generic type.",
			messageFormat: "Cannot apply ImmutableGeneric for the open generic type '{0}'.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "ImmutableGeneric can only be applied to closed (fully bound) generic types."
		);

		public static readonly DiagnosticDescriptor DontUseImmutableArrayConstructor = new DiagnosticDescriptor(
			id: "D2L0029",
			title: "Don't use the default constructor for ImmutableArray<T>",
			messageFormat: "The default constructor for ImmutableArray<T> doesn't correctly initialize the object and leads to runtime errors. Use ImmutableArray<T>.Empty for empty arrays, ImmutableArray.Create() for simple cases and ImmutableArray.Builder<T> for more complicated cases.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The default constructor for ImmutableArray<T> doesn't correctly initialize the object and leads to runtime errors. Use ImmutableArray<T>.Empty for empty arrays, ImmutableArray.Create() for simple cases and ImmutableArray.Builder<T> for more complicated cases."
		);

		public static readonly DiagnosticDescriptor UnnecessaryMutabilityAnnotation = new DiagnosticDescriptor(
			id: "D2L0030",
			title: "Unnecessary mutability annotation should be removed to keep the code base clean",
			messageFormat: "The {0} annotation is not necessary because {1} is immutable. Please remove this attribute to keep our code base clean.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Unnecessary mutability annotations should be removed to keep the code base clean"
		);

		public static readonly DiagnosticDescriptor DangerousUsageOfCorsHeaderAppender = new DiagnosticDescriptor(
			id: "D2L0031",
			title: "ICorsHeaderAppender should not be used, as it can introduce security vulnerabilities.",
			messageFormat: "ICorsHeaderAppender should not be used, as it can introduce security vulnerabilities.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "ICorsHeaderAppender should not be used, as it can introduce security vulnerabilities."
		);

		public static readonly DiagnosticDescriptor TooManyUnnamedArgs = new DiagnosticDescriptor(
			id: "D2L0032",
			title: "There are a lot of arguments here. Please use named arguments for readability.",
			messageFormat: "There are a lot of arguments here. Please use named arguments for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "There are a lot of arguments here. Please use named arguments for readability."
		);

		public static readonly DiagnosticDescriptor LiteralArgShouldBeNamed = new DiagnosticDescriptor(
			id: "D2L0033",
			title: "Literal arguments should be named for readability.",
			messageFormat: "The argument for the {0} parameter is a literal expression. It's often hard to tell what the parameter for the argument is at the call-site in this case. Please use a named argument for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Literal arguments should be named for readability."
		);

		public static readonly DiagnosticDescriptor ArgumentsWithInterchangableTypesShouldBeNamed = new DiagnosticDescriptor(
			id: "D2L0034",
			title: "Arguments that map to parameters with interchangable types should be named.",
			messageFormat: "The parameters {0} and {1} have interchangable types. There is a risk of not passing arguments to the right parameters. Please use named arguments for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "Arguments that map to parameters with interchangable types should be named."
		);
	}
}
