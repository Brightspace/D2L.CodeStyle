using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Helpers {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ConstantAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
				Diagnostics.NonConstantPassedToConstantParameter,
				Diagnostics.InvalidConstantType
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

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeParameter(
					ctx,
					(ParameterSyntax)ctx.Node,
					constantAttribute
				),
				SyntaxKind.Parameter
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeArgument(
					ctx,
					(ArgumentSyntax)ctx.Node,
					constantAttribute
				),
				SyntaxKind.Argument
			);
		}

		private static void AnalyzeParameter(
			SyntaxNodeAnalysisContext context,
			ParameterSyntax parameterSyntax,
			ISymbol constantAttribute
		) {
			var parameter = context.SemanticModel.GetDeclaredSymbol( parameterSyntax );

			// Parameter is somehow null, so do nothing
			if( parameter == null ) {
				return;
			}

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
				Diagnostic.Create(
					descriptor: Diagnostics.InvalidConstantType,
					location: parameterSyntax.GetLocation(),
					messageArgs: type.TypeKind
				)
			);
		}

		private static void AnalyzeArgument(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument,
			ISymbol constantAttribute
		) {
			// Get the associated parameter
			var parameter = argument.DetermineParameter(
				context.SemanticModel,
				allowParams: false
			);

			// Parameter is somehow null, so do nothing
			if( parameter == null ) {
				return;
			}

			// Parameter is not [Constant], so do nothing
			if( !HasAttribute( parameter, constantAttribute ) ) {
				return;
			}

			// Argument is a constant value, so do nothing
			if( context.SemanticModel.GetConstantValue( argument.Expression ).HasValue ) {
				return;
			}

			// Argument was defined as [Constant] already, so trust it
			var argumentSymbol = context.SemanticModel.GetSymbolInfo( argument.Expression ).Symbol;
			if( argumentSymbol != null && HasAttribute( argumentSymbol, constantAttribute ) ) {
				return;
			}

			// Argument is not constant, so report it
			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: Diagnostics.NonConstantPassedToConstantParameter,
					location: argument.GetLocation(),
					messageArgs: parameter.Name
				)
			);
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
