using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class PatternStringAttributeAnalyzer : DiagnosticAnalyzer {

		public const int RegexTimeoutMS = 50;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
				Diagnostics.PatternStringMustBeConstant,
				Diagnostics.PatternStringDoesNotMatch,
				Diagnostics.PatternStringInvalidPattern,
				Diagnostics.PatternStringEvaluationTimeout
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

			ConcurrentDictionary<string, Regex> regexCache = [];

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
		}

		private static void AnalyzeAssignment(
			OperationAnalysisContext context,
			ISimpleAssignmentOperation assignment,
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
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
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
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
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
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

		// AnalyzeAssignment, AnalyzeFieldInitializer, and AnalyzePropertyInitializer
		// from above all feed in to here to centralize the logic.
		private static void HandleAttributedTarget(
			OperationAnalysisContext context,
			ISymbol attributedSymbol,
			ITypeSymbol targetType,
			IOperation valueOperation,
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			// This only matters for strings, everything else should be ignored.
			if( targetType.SpecialType != SpecialType.System_String ) {
				return;
			}

			// We only care about targets annotated with [PatternString].
			AttributeData? patternStringInstance = attributedSymbol.GetAttributes().FirstOrDefault(
				attr => SymbolEqualityComparer.Default.Equals(
					attr.AttributeClass,
					patternStringAttribute
				)
			);
			// If it doesn't have the attribute, nothing to do
			if( patternStringInstance == null ) {
				return;
			}

			// Once we've determined that we have a [PatternString] target then we
			// attempt to evaluate the value of the string.
			ProcessConstantValue(
				context,
				valueOperation,
				patternStringInstance,
				regexCache
			);
		}

		private static void ProcessConstantValue(
			OperationAnalysisContext context,
			IOperation valueOperation,
			AttributeData patternStringInstance,
			ConcurrentDictionary<string, Regex> regexCache
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

			// Now that we have the value, we can evaluate it against the declared regex pattern.
			DoPatternMatching(
				context,
				patternStringValue,
				valueOperation.Syntax.GetLocation(),
				patternStringInstance,
				regexCache
			);
		}

		private static void DoPatternMatching(
			OperationAnalysisContext context,
			string patternStringValue,
			Location location,
			AttributeData patternStringData,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			if( !TryGetPatternStringArguments( patternStringData, out string pattern, out bool expectMatch ) ) {
				return;
			}

			// Try to get the cached regex, if it's not in the cache create it,
			// but only store it if it was successfully created.
			if( !regexCache.TryGetValue( pattern, out Regex? regex ) ) {
				// It wasn't in the cache, so make a new one
				try {
					regex = new Regex(
						pattern,
						RegexOptions.CultureInvariant,
						TimeSpan.FromMilliseconds( RegexTimeoutMS )
					);
				} catch ( ArgumentException ex ) {
					// The declared pattern is not a valid regex.
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
				regexCache[ pattern ] = regex;
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
							expectMatch ? "to match" : "to not match",
							pattern
						]
					)
				);
			}
		}

		private static bool TryGetPatternStringArguments(
			AttributeData patternStringData,
			out string pattern,
			out bool expectMatch
		) {
			ImmutableArray<TypedConstant> arguments = patternStringData.ConstructorArguments;

			// Confirm there are enough arguments
			string? patternValue = arguments.Length > 0
				? arguments[0].Value as string
				: null;

			// and confirm the arguments are of the correct type
			expectMatch = !( arguments.Length > 1 && arguments[1].Value is bool b )
				|| b;

			// and confirm the pattern is actually specified
			if( string.IsNullOrWhiteSpace( patternValue ) ) {
				pattern = "";
				return false;
			}

			// The above check confirms it's not null, so we can use ! here
			pattern = patternValue!;
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
}
