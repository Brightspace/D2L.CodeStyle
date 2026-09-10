using System.Collections.Concurrent;
using System.Collections.Immutable;
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
			Diagnostics.PatternStringOnNonStringType,
			Diagnostics.ReferenceToMethodWithAttributedParameterNotSupported
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

		// The above operation actions validate the assigned value; this symbol
		// action validates the declaration itself so that applying
		// [PatternString] to a non-string member is flagged once at its source.
		context.RegisterSymbolAction(
			ctx => AnalyzeParameter(
				ctx, (IParameterSymbol)ctx.Symbol,
				patternStringAttribute,
				regexCache
			),
			SymbolKind.Parameter
		);

		context.RegisterOperationAction(
			ctx => AnalyzeMethodReference(
				ctx,
				(IMethodReferenceOperation)ctx.Operation,
				patternStringAttribute
			),
			OperationKind.MethodReference
		);
	}

	private static void AnalyzeParameter(
		SymbolAnalysisContext context,
		IParameterSymbol parameter,
		INamedTypeSymbol patternStringAttribute,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) {
		bool reportedNonString = false;

		foreach( AttributeData attribute in parameter.GetAttributes() ) {
			if( !SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				patternStringAttribute
			) ) {
				continue;
			}

			// [PatternString] only makes sense on strings.
			if( parameter.Type.SpecialType != SpecialType.System_String
				&& !reportedNonString
			) {
				Location location = parameter.Locations.FirstOrDefault() ?? Location.None;

				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.PatternStringOnNonStringType,
						location: location,
						messageArgs: [ parameter.Type.ToDisplayString() ]
					)
				);

				reportedNonString = true;
			}

			if( GetRegexForAttribute( attribute, regexCache, out string? pattern, out string? message ) is null ) {
				SyntaxReference? reference = attribute.ApplicationSyntaxReference;
				Location location = reference is null
					? parameter.Locations.FirstOrDefault() ?? Location.None
					: Location.Create( reference.SyntaxTree, reference.Span );

				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.PatternStringInvalidPattern,
						location: location,
						messageArgs: [
							pattern,
							message
						]
					)
				);
			}
		}
	}

	private static void AnalyzeMethodReference(
		OperationAnalysisContext context,
		IMethodReferenceOperation operation,
		INamedTypeSymbol PatternStringAttributeT
	) {
		foreach( IParameterSymbol parameter in operation.Method.Parameters ) {
			if( !HasAttribute( parameter, PatternStringAttributeT ) ) {
				continue;
			}

			context.ReportDiagnostic(
				descriptor: Diagnostics.ReferenceToMethodWithAttributedParameterNotSupported,
				location: operation.Syntax.GetLocation(),
				messageArgs: ["PatternString"]
			);

			return;
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
		if( !constant.HasValue || constant.Value is null ) {
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
		if( GetRegexForAttribute( patternStringData, regexCache ) is not { } regex ) {
			return;
		}

		bool expectMatch = true;
		foreach( KeyValuePair<string, TypedConstant> argument in patternStringData.NamedArguments ) {
			switch( argument.Key ) {
				case "ExpectMatch":
					expectMatch = (bool)argument.Value.Value!;
					break;
			}
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
						regex,
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
						expectMatch ? "to match" : "to not match",
						regex
					]
				)
			);
		}
	}

	private static Regex? GetRegexForAttribute(
		AttributeData attributeData,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache
	) => GetRegexForAttribute( attributeData, regexCache, out _, out _ );

	private static Regex? GetRegexForAttribute(
		AttributeData attributeData,
		ConcurrentDictionary<string, Lazy<Regex>> regexCache,
		out string? pattern,
		out string? message 
	) {
		ImmutableArray<TypedConstant> arguments = attributeData.ConstructorArguments;

		pattern = arguments.Length > 0
			? arguments[ 0 ].Value as string
			: null;
		if( pattern is null || string.IsNullOrWhiteSpace( pattern ) ) {
			message = "Pattern is empty";
			return null;
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
			message = ex.Message;
			return null;
		}

		message = null;
		return regex;
	}

	private static bool HasAttribute(
		ISymbol symbol,
		INamedTypeSymbol attributeType
	) {
		return symbol.GetAttributes()
			.Any( attr => SymbolEqualityComparer.Default.Equals(
					attributeType,
					attr.AttributeClass
				)
			);
	}
}
