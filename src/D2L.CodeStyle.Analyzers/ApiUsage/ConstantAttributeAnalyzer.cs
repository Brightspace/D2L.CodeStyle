#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ConstantAttributeAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
				Diagnostics.NonConstantPassedToConstantParameter,
				Diagnostics.InvalidConstantType,
				Diagnostics.ReferenceToMethodWithConstantParameterNotSupport
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

			// The D2L.CodeStyle.Annotations reference is optional
			if ( constantAttribute == null || constantAttribute .Kind == SymbolKind.ErrorType ) {
				return;
			}

			context.RegisterSymbolAction(
				ctx => AnalyzeParameter(
					ctx,
					(IParameterSymbol)ctx.Symbol,
					constantAttribute
				),
				SymbolKind.Parameter
			);

			context.RegisterOperationAction(
				ctx => AnalyzeArgument(
					ctx,
					(IArgumentOperation)ctx.Operation,
					constantAttribute
				),
				OperationKind.Argument
			);

			context.RegisterOperationAction(
				ctx => AnalyzeConversion(
					ctx,
					(IConversionOperation)ctx.Operation,
					constantAttribute
				),
				OperationKind.Conversion
			);

			context.RegisterOperationAction(
				ctx => AnalyzeMethodReference(
					ctx,
					(IMethodReferenceOperation)ctx.Operation,
					constantAttribute
				),
				OperationKind.MethodReference
			);
		}

		private static void AnalyzeParameter(
			SymbolAnalysisContext context,
			IParameterSymbol parameter,
			ISymbol constantAttribute
		) {
			// Parameter is not [Constant], so do nothing
			if( !HasAttribute( parameter, constantAttribute ) ) {
				return;
			}

			var type = parameter.Type;

			// Special types (bool, enum, int, string, etc) are the only types
			// which might be constant, aside from type parameters filled with
			// special types
			if( TypeCanBeConstant( type.SpecialType ) ) {
				return;
			}

			// If the parameter's type is a type parameter, then whether
			// it can be constant depends on the type that gets filled;
			// We can't determine that here
			if( type.Kind == SymbolKind.TypeParameter ) {
				return;
			}

			// The current parameter type cannot be marked as [Constant]
			context.ReportDiagnostic(
				descriptor: Diagnostics.InvalidConstantType,
				location: parameter.Locations.First(),
				messageArgs: new object[] { type.TypeKind }
			);
		}

		private static void AnalyzeArgument(
			OperationAnalysisContext context,
			IArgumentOperation argument,
			ISymbol constantAttribute
		) {
			var parameter = argument.Parameter;

			// Parameter is not [Constant], so do nothing
			if( !HasAttribute( parameter, constantAttribute ) ) {
				return;
			}

			// Argument is a constant value, so do nothing
			if( argument.Value.ConstantValue.HasValue ) {
				return;
			}

			// Argument was defined as [Constant] already, so trust it
			var argumentSymbol = argument.SemanticModel.GetSymbolInfo( (argument.Syntax as ArgumentSyntax).Expression, context.CancellationToken ).Symbol;
			if( argumentSymbol != null && HasAttribute( argumentSymbol, constantAttribute ) ) {
				return;
			}

			// Argument is not constant, so report it
			context.ReportDiagnostic(
				descriptor: Diagnostics.NonConstantPassedToConstantParameter,
				location: argument.Syntax.GetLocation(),
				messageArgs: new[] { parameter.Name }
			);
		}

		private static void AnalyzeConversion(
			OperationAnalysisContext context,
			IConversionOperation conversion,
			INamedTypeSymbol constantAttribute
		) {

			IMethodSymbol @operator = conversion.OperatorMethod;
			if( @operator is null ) {
				return;
			}
			if( @operator.Parameters.Length != 1 ) {
				return;
			}

			// Operator parameter is not [Constant], so do nothing
			IParameterSymbol parameter = @operator.Parameters[ 0 ];
			if( !HasAttribute( parameter, constantAttribute ) ) {
				return;
			}

			// Operand is a constant value, so trust it
			IOperation operand = conversion.Operand;
			if( operand.ConstantValue.HasValue ) {
				return;
			}

			// Operand was defined as [Constant] already, so trust it
			ISymbol operandSymbol = conversion.SemanticModel.GetSymbolInfo( operand.Syntax ).Symbol;
			if( operandSymbol is not null && HasAttribute( operandSymbol, constantAttribute ) ) {
				return;
			}

			// Operand is not constant, so report it
			context.ReportDiagnostic(
				descriptor: Diagnostics.NonConstantPassedToConstantParameter,
				location: operand.Syntax.GetLocation(),
				messageArgs: new[] { parameter.Name }
			);
		}

		private static void AnalyzeMethodReference(
			OperationAnalysisContext context,
			IMethodReferenceOperation operation,
			INamedTypeSymbol constantAttribute
		) {

			foreach( IParameterSymbol parameter in operation.Method.Parameters ) {

				if( !HasAttribute( parameter, constantAttribute ) ) {
					continue;
				}

				context.ReportDiagnostic(
					descriptor: Diagnostics.ReferenceToMethodWithConstantParameterNotSupport,
					location: operation.Syntax.GetLocation()
				);

				return;
			}
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

		private static bool TypeCanBeConstant( SpecialType specialType ) {
			switch( specialType ) {
				case SpecialType.System_Enum:
				case SpecialType.System_Boolean:
				case SpecialType.System_Char:
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
				case SpecialType.System_Decimal:
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_String:
					return true;
				default:
					return false;
			}
		}
	}
}
