using Microsoft.CodeAnalysis;

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

		// Retired:
		// D2L0014 (SingletonIsntImmutable): "Classes marked as a singleton should be immutable.",

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
			title: "Unnecessary Mutability.(Un)Audited Attribute",
			messageFormat: "There is a Mutability.Audited or Mutability.Unaudited attribute on an immutable member. Remove the unnecessary attribute.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor TooManyUnnamedArgs = new DiagnosticDescriptor(
			id: "D2L0032",
			title: "Name arguments for readability.",
			messageFormat: "There are more than {0} unnamed arguments. Use named arguments for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "When a lot of arguments are used, they should be named for readability."
		);

		public static readonly DiagnosticDescriptor LiteralArgShouldBeNamed = new DiagnosticDescriptor(
			id: "D2L0033",
			title: "Literal arguments should be named for readability.",
			messageFormat: "The argument for the {0} parameter is a literal expression. It's often hard to tell what the parameter for the argument is at the call-site in this case. Please use a named argument for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Info, // TODO: Change to Error once fixed in LMS
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

		public static readonly DiagnosticDescriptor SingletonDependencyHasCustomerState = new DiagnosticDescriptor(
			id: "D2L0035",
			title: "Singleton holding a dependency containing customer state.",
			messageFormat: "This class is marked as a singleton and holds a dependency with customer state.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Classes marked as singleton or that implement interfaces marked as a singleton cannot hold dependencies with customer state."
	   );

		public static readonly DiagnosticDescriptor PublicClassHasHiddenCustomerState = new DiagnosticDescriptor(
			id: "D2L0036",
			title: "Missing CustomerState attribute.",
			messageFormat: "Public class holding private customer state dependency.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Classes visible to singletons that are not public but contain dependencies that have customer state must be marked with [CustomerState] to facilitate cross-assembly analysis."
	   );

		public static readonly DiagnosticDescriptor GenericArgumentImmutableMustBeApplied = new DiagnosticDescriptor(
			id: "D2L0037",
			title: "Missing immutability attribute.",
			messageFormat: "Generic argument must be marked immutable.",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Either an interface, base class or constraint indicates the generic argument must be immutable. Add [Immutable] to the generic argument in the class definintion."
	   );

		public static readonly DiagnosticDescriptor GenericArgumentTypeMustBeImmutable = new DiagnosticDescriptor(
			id: "D2L0038",
			title: "Declared type is not immutable.",
			messageFormat: "Generic argument type '{0}' must be marked with [Immutable].",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "A generic argument is marked [Immutable] but the type supplied was not immutable."
	   );

		public static readonly DiagnosticDescriptor DangerousPropertiesShouldBeAvoided = new DiagnosticDescriptor(
			id: "D2L0039",
			title: "Avoid using dangerous properties",
			messageFormat: "Should not use {0} because it's considered dangerous",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Avoid using of dangerous properties"
		);

		public static readonly DiagnosticDescriptor MissingTransitiveImmutableAttribute = new DiagnosticDescriptor(
			id: "D2L0040",
			title: "Missing an explicit transitive [Immutable] attribute",
			messageFormat: "{0} should be [Immutable] because the {1} {2} is.",
			category: "",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The implications of [Immutable] apply transitively to derived classes and interface implementations. We require that [Immutable] is explicity applied transitively for clarity and simplicity."
		);

		public static readonly DiagnosticDescriptor EventHandlerDisallowed = new DiagnosticDescriptor(
			id: "D2L0041",
			title: "Disallowed Event Handler",
			messageFormat: "Event handlers of type {0} have been disallowed",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "This event type no longer supports event handlers."
		);

		public static readonly DiagnosticDescriptor MustReferenceAnnotations = new DiagnosticDescriptor(
			id: "D2L0042",
			title: "To use D2L.CodeStyle.Analyzers you must also reference the assembly D2L.CodeStyle.Annotations",
			messageFormat: "To use D2L.CodeStyle.Analyzers you must also reference the assembly D2L.CodeStyle.Annotations",
			category: "Build",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "To use D2L.CodeStyle.Analyzers you must also reference the assembly D2L.CodeStyle.Annotations"
		);

		public static readonly DiagnosticDescriptor EventTypeMissingEventAttribute = new DiagnosticDescriptor(
			id: "D2L0043",
			title: "Event Type Missing [Event] Attribute",
			description: "All event types must be marked with [Event] attribute.",
			messageFormat: "Event type {0} must be marked with [Event] attribute.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor EventHandlerTypeMissingEventAttribute = new DiagnosticDescriptor(
			id: "D2L0044",
			title: "Event Handler Type Missing [EventHandler] Attribute",
			description: "All event handler types must be marked with [EventHandler] attribute.",
			messageFormat: "Event handler type {0} must be marked with [EventHandler] attribute.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor EventTypeMissingImmutableAttribute = new DiagnosticDescriptor(
			id: "D2L0045",
			title: "Event Type Missing [Immutable] Attribute",
			messageFormat: "{0} must be marked [Immutable] because all event types must be immutable.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "We require that [Immutable] be explicity applied to all event types."
		);

    public static readonly DiagnosticDescriptor DependencyRegistraionMissingPublicConstructor = new DiagnosticDescriptor(
			id: "D2L0046",
			title: "Dependency Registration Missing Public Constructor",
			messageFormat: "{0} must have a public constructor if it is to be registered for DI.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "All injectable types need a public constructor in order to be activated."
    );

		public static readonly DiagnosticDescriptor IncludeDefaultValueInOverrideForReadability = new DiagnosticDescriptor(
			id: "D2L0047",
			title: "The parameter {0} has a default value in {1}, but not here. This causes inconsistent behaviour and reduces readability. Please repeat the default value here explicitly.",
			messageFormat: "The parameter {0} has a default value in {1}, but not here. This causes inconsistent behaviour and reduces readability. Please repeat the default value here explicitly.",
			category: "Language",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The parameter {0} has a default value in {1}, but not here. This causes inconsistent behaviour and reduces readability. Please repeat the default value here explicitly."
		);

		public static readonly DiagnosticDescriptor DontIntroduceNewDefaultValuesInOverrides = new DiagnosticDescriptor(
			id: "D2L0048",
			title: "The parameter {0} does not have a default value in the original version of this method in {1}, but does here. This causes inconsistent behaviour. Please remove the default (or add it everywhere.)",
			messageFormat: "The parameter {0} does not have a default value in the original version of this method in {1}, but does here. This causes inconsistent behaviour. Please remove the default (or add it everywhere.)",
			category: "Language",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The parameter {0} does not have a default value in the original version of this method in {1}, but does here. This causes inconsistent behaviour. Please remove the default (or add it everywhere.)"
		);

		public static readonly DiagnosticDescriptor DefaultValuesInOverridesShouldBeConsistent = new DiagnosticDescriptor(
			id: "D2L0049",
			title: "The parameter {0} has a default value of {1} here, but {2} in its original definition in {3}. This causes inconsistent behaviour. Please use the same defualt value everywhere.",
			messageFormat: "The parameter {0} has a default value of {1} here, but {2} in its original definition in {3}. This causes inconsistent behaviour. Please use the same defualt value everywhere.",
			category: "Language",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The parameter {0} has a default value of {1} here, but {2} in its original definition in {3}. This causes inconsistent behaviour. Please use the same defualt value everywhere."
		);

		public static readonly DiagnosticDescriptor LoggingContextRunAwaitable = new DiagnosticDescriptor(
			id: "D2L0050",
			title: "Use RunAsync for awaitable actions.",
			messageFormat: "Use RunAsync for awaitable actions.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Use RunAsync for awaitable actions."
		);

		public static readonly DiagnosticDescriptor BannedConfig = new DiagnosticDescriptor(
			id: "D2L0051",
			title: "Fetching the config variable \"{0}\" has been deprecated. {1}",
			messageFormat: "Fetching the config variable \"{0}\" has been deprecated. {1}",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Fetching the config variable \"{0}\" has been deprecated. {1}"
		);

		public static readonly DiagnosticDescriptor ContentPhysicalPathUsages = new DiagnosticDescriptor(
			id: "D2L0052",
			title: "Use the ContentPath property.",
			messageFormat: "Use the ContentPath property instead.",
			category: "Storageable",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Use the ContentPath property instead."
		);

		public static readonly DiagnosticDescriptor StatelessFuncIsnt = new DiagnosticDescriptor(
			id: "D2L0053",
			title: "StatelessFunc cannot hold state",
			messageFormat: "StatelessFunc cannot hold state: {0}",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "StatelessFunc is to be used to hold Func private members, and need to be undiff safe."
		);

		public static readonly DiagnosticDescriptor ReadOnlyParameterIsnt = new DiagnosticDescriptor(
			id: "D2L0054",
			title: "Parameter is not readonly",
			messageFormat: "Parameter is not readonly: {0}",
			category: "Safety",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "[ReadOnly] paramaters must not be assigned to or passed by non-readonly reference."
		);

		public static readonly DiagnosticDescriptor AwaitedTaskNotConfigured = new DiagnosticDescriptor(
			id: "D2L0055",
			title: "Awaited task is not configured",
			messageFormat: "Awaited task is not configured",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Awaited task should have 'continueOnCapturedContext' configured (preferably with 'false')."
		);

		public static readonly DiagnosticDescriptor StructShouldBeReadonly = new DiagnosticDescriptor(
			id: "D2L0056",
			title: "Struct should be readonly",
			messageFormat: "Struct '{0}' should be marked as readonly",
			description: "A struct without mutable fields or properties should be marked readonly. A non-readonly struct other weird mutability behaviours and performance pitfalls.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor UnnecessaryAllowedListEntry = new DiagnosticDescriptor(
			id: "D2L0057",
			title: "Unnecessarily listed in an analyzer allowed list",
			messageFormat: "The entry for '{0}' in '{1}' is unnecessary.",
			category: "Cleanliness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Unnecessarily listed in an analyzer allowed list."
		 );

		public static readonly DiagnosticDescriptor NamedArgumentsRequired = new DiagnosticDescriptor(
			id: "D2L0058",
			title: "Named arguments are required for readability.",
			messageFormat: "Named arguments are required for this call. Use named arguments for readability.",
			category: "Readability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "This call requires arguments be named for readability."
		);

		public static readonly DiagnosticDescriptor InvalidSerializerType = new DiagnosticDescriptor(
			id: "D2L0059",
			title: "Serializers must implement D2L.LP.Serialization.ITrySerializer.",
			messageFormat: "'{0}' does not implement D2L.LP.Serialization.ITrySerializer.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "This [Serializer] attribute requires the provided type to implement D2L.LP.Serialization.ITrySerializer."
		);

		public static readonly DiagnosticDescriptor EventTypeNotSealed = new DiagnosticDescriptor(
			id: "D2L0060",
			title: "Event Type Not Sealed",
			messageFormat: "{0} must be sealed because you can register event handlers based on inheritance.",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "We require that event types be sealed."
		);

		public static readonly DiagnosticDescriptor MutableBaseType = new DiagnosticDescriptor(
			id: "D2L0061",
			title: "Base class for immutable type must be [Immutable] (or [ImmutableBaseClass])",
			messageFormat: "{0}'s base class is {1} which is missing [Immutable] (or, more weakly, [ImmutableBaseClass]).",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor ArraysAreMutable = new DiagnosticDescriptor(
			id: "D2L0062",
			title: "Arrays are mutable and thus can't be held by an immutable type.",
			messageFormat: "{0}[]'s are mutable even if {0}'s are immutable because array items can be substituted. Prefer an immutable container type such as ImmutableArray<{0}>.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor DynamicObjectsAreMutable = new DiagnosticDescriptor(
			id: "D2L0063",
			title: "Dynamic objects are mutable and thus can't be held by an immutable type.",
			messageFormat: "Dynamic objects are mutable and thus can't be held by an immutable type.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor TypeParameterIsNotKnownToBeImmutable = new DiagnosticDescriptor(
			id: "D2L0064",
			title: "Type parameter is not known to be immutable.",
			messageFormat: "The type parameter {0} is not known to be immutable. Add [Immutable] to it.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor UnexpectedTypeKind = new DiagnosticDescriptor(
			id: "D2L0065",
			title: "Unsupported type kind inside an immutable type declaration.",
			messageFormat: "The type kind {0} is unexpected/unhandled by the immutability analyzer, and for safety we are emitting an error. This is a bug in the analyzer.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			helpLinkUri: "https://github.com/Brightspace/D2L.CodeStyle/issues/new?title=Unexpected%20type%20kind%20in%20immutability%20analysis&labels=bug"
		);

		public static readonly DiagnosticDescriptor NonImmutableTypeHeldByImmutable = new DiagnosticDescriptor(
			id: "D2L0066",
			title: "Type may be mutable.",
			messageFormat: "The {0} {1} is missing the [Immutable]{2} attribute, so it isn't safe to be held by an immutable type",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor MemberIsNotReadOnly = new DiagnosticDescriptor(
			id: "D2L0067",
			title: "Member must be readonly inside immutable type.",
			messageFormat: "The {0} {1} must be readonly because {2} is immutable.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor UnexpectedMemberKind = new DiagnosticDescriptor(
			id: "D2L0068",
			title: "Unsupported member type in immutable type declaration.",
			messageFormat: "The member {0} is of type {1} which is not supported by the immutability analyzer. This is a bug.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			helpLinkUri: "https://github.com/Brightspace/D2L.CodeStyle/issues/new?title=Unexpected%20member%20kind%20in%20immutability%20analysis&labels=bug"
		);

		public static readonly DiagnosticDescriptor DelegateTypesPossiblyMutable = new DiagnosticDescriptor(
			id: "D2L0069",
			title: "Delegates members are not allowed in immutable types.",
			messageFormat: "Delegate fields and properties can close over mutable state. For safety, we don't allow these in immutable types.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor EventMemberMutable = new DiagnosticDescriptor(
			id: "D2L0070",
			title: "Event members are not allowed in immtuable types.",
			messageFormat: "Event members hold registered event handlers. We consider this to be a source of mutability.",
			category: "Immutability",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
	}
}
