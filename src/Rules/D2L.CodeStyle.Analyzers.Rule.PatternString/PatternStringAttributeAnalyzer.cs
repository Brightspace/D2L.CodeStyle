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
			var constantAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.Contract.ConstantAttribute"
			);
			var patternStringAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.Contract.PatternStringAttribute"
			);

			// If we couldn't find the symbols don't proceed with the evaluation.
			if( constantAttribute is null
				|| constantAttribute.Kind == SymbolKind.ErrorType
				|| patternStringAttribute is null
				|| patternStringAttribute.Kind == SymbolKind.ErrorType
			) {
				return;
			}

			ConcurrentDictionary<string, Regex> regexCache = [];

			context.RegisterOperationAction(
				ctx => AnalyzeAssignment(
					ctx,
					(ISimpleAssignmentOperation)ctx.Operation,
					constantAttribute,
					patternStringAttribute,
					regexCache
				),
				OperationKind.SimpleAssignment
			);

			context.RegisterOperationAction(
				ctx => AnalyzeFieldInitializer(
					ctx,
					(IFieldInitializerOperation)ctx.Operation,
					constantAttribute,
					patternStringAttribute,
					regexCache
				),
				OperationKind.FieldInitializer
			);

			context.RegisterOperationAction(
				ctx => AnalyzePropertyInitializer(
					ctx,
					(IPropertyInitializerOperation)ctx.Operation,
					constantAttribute,
					patternStringAttribute,
					regexCache
				),
				OperationKind.PropertyInitializer
			);
		}

		private static void AnalyzeAssignment(
			OperationAnalysisContext context,
			ISimpleAssignmentOperation assignment,
			ISymbol constantAttribute,
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
				constantAttribute: constantAttribute,
				patternStringAttribute: patternStringAttribute,
				regexCache: regexCache
			);
		}

		private static void AnalyzeFieldInitializer(
			OperationAnalysisContext context,
			IFieldInitializerOperation initializer,
			ISymbol constantAttribute,
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			foreach( IFieldSymbol field in initializer.InitializedFields ) {
				HandleAttributedTarget(
					context,
					attributedSymbol: field,
					targetType: field.Type,
					valueOperation: initializer.Value,
					constantAttribute: constantAttribute,
					patternStringAttribute: patternStringAttribute,
					regexCache: regexCache
				);
			}
		}

		private static void AnalyzePropertyInitializer(
			OperationAnalysisContext context,
			IPropertyInitializerOperation initializer,
			ISymbol constantAttribute,
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			foreach( IPropertySymbol property in initializer.InitializedProperties ) {
				HandleAttributedTarget(
					context,
					attributedSymbol: property,
					targetType: property.Type,
					valueOperation: initializer.Value,
					constantAttribute: constantAttribute,
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
			ISymbol constantAttribute,
			ISymbol patternStringAttribute,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			// This only matters for strings, everything else should be ignored.
			if( targetType.SpecialType != SpecialType.System_String
			) {
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

			// We will only analyze symbols with [PatternString] if it is also
			// marked with [Constant].
			if( !HasAttribute( attributedSymbol, constantAttribute ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.PatternStringMustBeConstant,
						location: attributedSymbol.Locations.First(),
						messageArgs: []
					)
				);
			}

			// Once we've determine that we have a symbol and it's appropriately
			// decorated then we attempt to evaluate the value of string.
			ProcessConstantValue( context, valueOperation, patternStringInstance, regexCache );
		}

		private static void ProcessConstantValue(
			OperationAnalysisContext context,
			IOperation valueOperation,
			AttributeData patternStringInstance,
			ConcurrentDictionary<string, Regex> regexCache
		) {
			// The [Constant] attribute guarantees that the value is a compile-time
			// constant, so we can read its constant value directly off the operation.
			Optional<object?> constant = valueOperation.ConstantValue;
			if( !constant.HasValue ) {
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
				regexCache.TryAdd( pattern, regex );
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

			string? patternValue = arguments.Length > 0
				? arguments[0].Value as string
				: null;

			expectMatch = !( arguments.Length > 1 && arguments[1].Value is bool b )
				|| b;

			if( string.IsNullOrWhiteSpace( patternValue ) ) {
				pattern = "";
				return false;
			}

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


		/// <summary>
		/// Check if the symbol has a specific attribute attached to it.
		/// </summary>
		/// <param name="symbol">The symbol to check for an attribute on</param>
		/// <param name="attributeSymbol">The symbol of the attribute</param>
		/// <returns>True if the attribute exists on the symbol, false otherwise</returns>
		private static bool HasAttribute(
			ISymbol symbol,
			ISymbol attributeSymbol
		) {
			return symbol.GetAttributes()
				.Any( attr => SymbolEqualityComparer.Default.Equals(
						attributeSymbol,
						attr.AttributeClass
					)
				);
		}

		private static bool IsMockArgumentConstraint(
			IOperation operation
		) {
			// Matches patterns like Arg<T>.Is.Anything or Arg<T>.Is.Equal(...) (Rhino Mocks)
			// Arg<T>.Is.Anything is a property chain: Arg<string>.Is (static) -> .Anything (instance)
			// Arg<T>.Is.Equal(...) is a method call on the .Is property result
			if( operation is IInvocationOperation invocation ) {
				var containingType = invocation.TargetMethod.ContainingType;
				if( IsArgType( containingType ) ) {
					return true;
				}
				// Check if the instance receiver is an Arg<T> property chain
				if( invocation.Instance != null && IsMockArgumentConstraint( invocation.Instance ) ) {
					return true;
				}
			}

			IOperation? current = operation;
			while( current is IPropertyReferenceOperation propertyRef ) {
				var containingType = propertyRef.Property.ContainingType;
				if( IsArgType( containingType ) ) {
					return true;
				}
				current = propertyRef.Instance;
			}
			return false;
		}

		private static bool IsArgType( INamedTypeSymbol type ) {
			if( type == null ) {
				return false;
			}
			if( type.Name == "Arg" && type.IsGenericType ) {
				return true;
			}
			// Check containing types for nested types within Arg<T>
			return IsArgType( type.ContainingType );
		}
	}
}
