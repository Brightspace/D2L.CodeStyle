using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage;

[DiagnosticAnalyzer( LanguageNames.CSharp )]
public sealed class PatternStringAttributeAnalyzer : DiagnosticAnalyzer {

	public const int RegexTimeoutMS = 750;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(
			Diagnostics.PatternStringMustBeConstant,
			Diagnostics.PatternStringDoesNotMatch,
			Diagnostics.PatternStringInvalidPattern,
			Diagnostics.PatternStringEvaluationTimeout,
			Diagnostics.PatternStringOnNonStringType
		);

	public override void Initialize( AnalysisContext context ) {
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
		context.RegisterCompilationStartAction( CompilationStart );
	}

	public static void CompilationStart(
		CompilationStartAnalysisContext context
	) {
		var patternStringAttribute = context.Compilation.GetTypeByMetadataName(
			"D2L.CodeStyle.Annotations.Contract.PatternStringAttribute"
		);

		// If we couldn't find the symbol don't proceed with the evaluation.
		if( patternStringAttribute is null
			|| patternStringAttribute.Kind == SymbolKind.ErrorType
		) {
			return;
		}

		ConcurrentDictionary<string, Lazy<Regex>> regexCache = [];

		context.RegisterOperationAction(
			ctx => AnalyzeAssignment(
				ctx,
				(ISimpleAssignmentOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.SimpleAssignment
		);

		context.RegisterOperationAction(
			ctx => AnalyzeFieldInitializer(
				ctx,
				(IFieldInitializerOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.FieldInitializer
		);

		context.RegisterOperationAction(
			ctx => AnalyzePropertyInitializer(
				ctx,
				(IPropertyInitializerOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.PropertyInitializer
		);

		context.RegisterOperationAction(
			ctx => AnalyzeArgument(
				ctx,
				(IArgumentOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.Argument
		);

		context.RegisterOperationAction(
			ctx => AnalyzeConversion(
				ctx,
				(IConversionOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.Conversion
		);

		context.RegisterOperationAction(
			ctx => AnalyzeDeconstructionAssignment(
				ctx,
				(IDeconstructionAssignmentOperation)ctx.Operation,
				patternStringAttribute,
				regexCache
			),
			OperationKind.DeconstructionAssignment
		);

		// The above operation actions validate the assigned value; this symbol
		// action validates the declaration itself so that applying
		// [PatternString] to a non-string member is flagged once at its source.
		context.RegisterSymbolAction(
			ctx => AnalyzeSymbolDeclaration( ctx, patternStringAttribute ),
			SymbolKind.Field,
			SymbolKind.Property,
			SymbolKind.Method
		);
	}

	private static void AnalyzeSymbolDeclaration(
		SymbolAnalysisContext context,
		ISymbol patternStringAttribute
	) {
		switch( context.Symbol ) {
			case IFieldSymbol field:
				ReportIfNonStringTarget( context, field, field.Type, patternStringAttribute );
				break;
			case IPropertySymbol property:
				ReportIfNonStringTarget( context, property, property.Type, patternStringAttribute );
				break;
			case IMethodSymbol method:
				foreach( IParameterSymbol parameter in method.Parameters ) {
					ReportIfNonStringTarget( context, parameter, parameter.Type, patternStringAttribute );
				}
				break;
		}
	}

	private static void ReportIfNonStringTarget(
		SymbolAnalysisContext context,
		ISymbol attributedSymbol,
		ITypeSymbol targetType,
		ISymbol patternStringAttribute
	) {
		// [PatternString] only makes sense on strings.
		if( targetType.SpecialType == SpecialType.System_String ) {
			return;
		}

		foreach( AttributeData attribute in attributedSymbol.GetAttributes() ) {
			if( !SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				patternStringAttribute
			) ) {
				continue;
			}

			SyntaxReference? reference = attribute.ApplicationSyntaxReference;
			Location location = reference is null
				? attributedSymbol.Locations.FirstOrDefault() ?? Location.None
				: Location.Create( reference.SyntaxTree, reference.Span );

			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.PatternStringOnNonStringType,
					location: location,
					messageArgs: [ targetType.ToDisplayString() ]
				)
			);
		}
	}

	private static void AnalyzeAssignment(
		OperationAnalysisContext context,
		ISimpleAssignmentOperation assignment,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		ISymbol target;
		ITypeSymbol targetType;

		switch( assignment.Target ) {
			case IPropertyReferenceOperation propertyReference:
				target = propertyReference.Property;
				targetType = propertyReference.Property.Type;
				break;
			case IFieldReferenceOperation fieldReference:
				target = fieldReference.Field;
				targetType = fieldReference.Field.Type;
				break;
			default:
				return;
		}

		HandleAttributedTarget(
			context,
			attributedSymbol: target,
			targetType: targetType,
			valueOperation: assignment.Value,
			patternStringAttribute: patternStringAttribute,
			regexCache: regexCache
		);
	}

	private static void AnalyzeFieldInitializer(
		OperationAnalysisContext context,
		IFieldInitializerOperation initializer,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		foreach( IFieldSymbol field in initializer.InitializedFields ) {
			HandleAttributedTarget(
				context,
				attributedSymbol: field,
				targetType: field.Type,
				valueOperation: initializer.Value,
				patternStringAttribute: patternStringAttribute,
				regexCache: regexCache
			);
		}
	}

	private static void AnalyzePropertyInitializer(
		OperationAnalysisContext context,
		IPropertyInitializerOperation initializer,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		foreach( IPropertySymbol property in initializer.InitializedProperties ) {
			HandleAttributedTarget(
				context,
				attributedSymbol: property,
				targetType: property.Type,
				valueOperation: initializer.Value,
				patternStringAttribute: patternStringAttribute,
				regexCache: regexCache
			);
		}
	}

	private static void AnalyzeArgument(
		OperationAnalysisContext context,
		IArgumentOperation argument,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		IParameterSymbol? parameter = argument.Parameter;
		if( parameter is null ) {
			return;
		}

		HandleAttributedTarget(
			context,
			attributedSymbol: parameter,
			targetType: parameter.Type,
			valueOperation: argument.Value,
			patternStringAttribute: patternStringAttribute,
			regexCache: regexCache
		);
	}

	private static void AnalyzeConversion(
		OperationAnalysisContext context,
		IConversionOperation conversion,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		// Only user-defined conversion operators can carry a [PatternString]
		// attribute on their parameter.
		IMethodSymbol? conversionMethod = conversion.OperatorMethod;
		if( conversionMethod is null || conversionMethod.Parameters.Length != 1 ) {
			return;
		}

		IParameterSymbol parameter = conversionMethod.Parameters[ 0 ];

		HandleAttributedTarget(
			context,
			attributedSymbol: parameter,
			targetType: parameter.Type,
			valueOperation: conversion.Operand,
			patternStringAttribute: patternStringAttribute,
			regexCache: regexCache
		);
	}

	private static void AnalyzeDeconstructionAssignment(
		OperationAnalysisContext context,
		IDeconstructionAssignmentOperation assignment,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		HandleDeconstruction(
			context,
			target: assignment.Target,
			value: assignment.Value,
			patternStringAttribute: patternStringAttribute,
			regexCache: regexCache
		);
	}

	// Handle deconstruction operations like (foo.Property, foo.Field) = ("123", "456");
	// and ensure it carries through to the validation.
	private static void HandleDeconstruction(
		OperationAnalysisContext context,
		IOperation target,
		IOperation value,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		// First, on each target/value unwrap them in case they're using implicit
		// conversions, then confirm that we're going tuple<->tuple
		if( ( UnwrapConversions( target ) is not ITupleOperation targetTuple )
			|| ( UnwrapConversions( value ) is not ITupleOperation valueTuple )
		) {
			return;
		}

		// Now confirm we're deconstructing to tuples of the same size
		// otherwise this doesn't allow us to pair up below.
		if( targetTuple.Elements.Length != valueTuple.Elements.Length ) {
			return;
		}

		for( int i = 0; i < targetTuple.Elements.Length; i++ ) {
			IOperation elementTarget = UnwrapConversions( targetTuple.Elements[ i ] );
			IOperation elementValue = valueTuple.Elements[ i ];

			// Nested deconstruction, e.g. ((a, b), c) = ((x, y), z).
			if( elementTarget is ITupleOperation ) {
				HandleDeconstruction(
					context,
					target: elementTarget,
					value: elementValue,
					patternStringAttribute: patternStringAttribute,
					regexCache: regexCache
				);
				continue;
			}

			// Comb out the member and memberType we're interested in
			// so HandleAttributedTarget doesn't care where it came from.
			ISymbol member;
			ITypeSymbol memberType;
			switch( elementTarget ) {
				case IPropertyReferenceOperation propertyReference:
					member = propertyReference.Property;
					memberType = propertyReference.Property.Type;
					break;
				case IFieldReferenceOperation fieldReference:
					member = fieldReference.Field;
					memberType = fieldReference.Field.Type;
					break;
				default:
					continue;
			}

			HandleAttributedTarget(
				context,
				attributedSymbol: member,
				targetType: memberType,
				valueOperation: UnwrapConversions( elementValue ),
				patternStringAttribute: patternStringAttribute,
				regexCache: regexCache
			);
		}
	}

	private static IOperation UnwrapConversions(
		IOperation operation
	) {
		// Keep peeling away conversions until we get to the underlying operation
		while( operation is IConversionOperation conversion
			&& conversion.OperatorMethod is null
		) {
			operation = conversion.Operand;
		}

		return operation;
	}

	// AnalyzeAssignment, AnalyzeFieldInitializer, AnalyzePropertyInitializer,
	// and AnalyzeArgument from above all feed in to here to centralize the logic.
	private static void HandleAttributedTarget(
		OperationAnalysisContext context,
		ISymbol attributedSymbol,
		ITypeSymbol targetType,
		IOperation valueOperation,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		// Non-string targets are reported at their declaration, bail here
		// so we don't attempt to treat a non-string constant as a string.
		if( targetType.SpecialType != SpecialType.System_String ) {
			return;
		}

		// We only care about targets annotated with [PatternString]. Since
		// the attribute may be specified multiple times we collect all of them.
		ImmutableArray<AttributeData> patternStringInstances = attributedSymbol
			.GetAttributes()
			.Where( a => SymbolEqualityComparer.Default.Equals( a.AttributeClass, patternStringAttribute ) )
			.ToImmutableArray();
		// If it doesn't have the attribute, nothing to do
		if( patternStringInstances.IsEmpty ) {
			return;
		}

		// Once we've determined that we have a [PatternString] target then we
		// attempt to evaluate the value of the string.
		ProcessConstantValue(
			context,
			valueOperation,
			patternStringInstances,
			regexCache
		);
	}

	private static void ProcessConstantValue(
		OperationAnalysisContext context,
		IOperation valueOperation,
		ImmutableArray<AttributeData> patternStringInstances,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		// [PatternString] requires the assigned value to be a compile-time
		// constant so that we can evaluate it here.
		Optional<object?> constant = valueOperation.ConstantValue;
		if( !constant.HasValue ) {
			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.PatternStringMustBeConstant,
					location: valueOperation.Syntax.GetLocation(),
					messageArgs: []
				)
			);
			return;
		}

		// We can use ! here because the above check ensures there's a value
		string patternStringValue = (string)constant.Value!;

		// Now that we have the value, we can evaluate it against every declared
		// regex pattern. A diagnostic is emitted for each pattern that fails.
		foreach( AttributeData patternStringInstance in patternStringInstances ) {
			DoPatternMatching(
				context,
				patternStringValue,
				valueOperation.Syntax.GetLocation(),
				patternStringInstance,
				regexCache
			);
		}
	}

	private static void DoPatternMatching(
		OperationAnalysisContext context,
		string patternStringValue,
		Location location,
		AttributeData patternStringData,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		if( !TryGetPatternStringArguments( patternStringData, out string? pattern, out bool? expectMatch ) ) {
			return;
		}

		// Get (or lazily construct) the cached regex. Using a Lazy<Regex>
		// with GetOrAdd guarantees the regex is compiled exactly once per
		// pattern, even when multiple threads miss the cache concurrently.
		Lazy<Regex> lazyRegex = regexCache.GetOrAdd(
			pattern,
			static ( string p ) => new Lazy<Regex>(
				() => new Regex(
					p,
					RegexOptions.CultureInvariant,
					TimeSpan.FromMilliseconds( RegexTimeoutMS )
				),
				LazyThreadSafetyMode.ExecutionAndPublication
			)
		);

		Regex regex;
		try {
			regex = lazyRegex.Value;
		} catch( ArgumentException ex ) {
			// The declared pattern is not a valid regex. The exception is
			// cached by the Lazy, so we don't repeatedly attempt to compile
			// an invalid pattern.
			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.PatternStringInvalidPattern,
					location: GetPatternDiagnosticLocation( patternStringData, location ),
					messageArgs: [
						pattern,
						ex.Message
					]
				)
			);
			return;
		}

		bool matched;
		try {
			matched = regex.IsMatch( patternStringValue );
		} catch( RegexMatchTimeoutException ) {
			// The evaluation of the pattern against the value exceeded the
			// allotted time budget.
			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.PatternStringEvaluationTimeout,
					location: location,
					messageArgs: [
						pattern,
						patternStringValue,
						RegexTimeoutMS
					]
				)
			);
			return;
		}

		// Check against the expectMatch of the attribute to confirm
		// the result is what the declaration expected.
		if( matched != expectMatch ) {
			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.PatternStringDoesNotMatch,
					location: location,
					messageArgs: [
						patternStringValue,
						expectMatch.Value ? "to match" : "to not match",
						pattern
					]
				)
			);
		}
	}

	private static bool TryGetPatternStringArguments(
		AttributeData patternStringData,
		[NotNullWhen( true )] out string? pattern,
		[NotNullWhen( true )] out bool? expectMatch
	) {
		ImmutableArray<TypedConstant> arguments = patternStringData.ConstructorArguments;

		pattern = arguments.Length > 0
			? arguments[ 0 ].Value as string
			: null;
		if( pattern is null || string.IsNullOrWhiteSpace( pattern ) ) {
			pattern = null;
			expectMatch = false;
			return false;
		}

		expectMatch = true;

		foreach( KeyValuePair<string, TypedConstant> argument in patternStringData.NamedArguments ) {
			switch( argument.Key ) {
				case "ExpectMatch":
					expectMatch = (bool)argument.Value.Value!;
					break;
			}
		}

		return true;
	}

	/// <summary>
	/// Prefers the location of the [PatternString] attribute application (so the
	/// invalid pattern is flagged on the declaration) and falls back to the value
	/// location when the attribute originates from metadata.
	/// </summary>
	private static Location GetPatternDiagnosticLocation(
		AttributeData patternStringData,
		Location fallback
	) {
		SyntaxReference? reference = patternStringData.ApplicationSyntaxReference;
		if( reference == null ) {
			return fallback;
		}

		return Location.Create( reference.SyntaxTree, reference.Span );
	}
}
