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

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeParameters(
					ctx,
					(ParameterSyntax)ctx.Node,
					constantAttribute
				),
				SyntaxKind.Parameter
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeArguments(
					ctx,
					(ArgumentSyntax)ctx.Node,
					constantAttribute
				),
				SyntaxKind.Argument
			);
		}

		private static void AnalyzeParameters(
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
			if( type.SpecialType != SpecialType.None ) {
				return;
			}

			// If the parameter is a type parameter, there aren't issues yet...
			// There may be issues when the method is being called, but
			// those are handled in the argument analyzer
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

		private static void AnalyzeArguments(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument,
			ISymbol constantAttribute
		) {
			// Get the associated parameter
			var parameter = argument.DetermineParameter(
				context.SemanticModel,
				allowParams: true
			);

			// Parameter is somehow null, so do nothing
			if( parameter == null ) {
				return;
			}

			// Parameter is not [Constant], so do nothing
			if( !HasAttribute( parameter, constantAttribute ) ) {
				return;
			}

			var type = context.SemanticModel.GetTypeInfo( argument.Expression ).Type;

			// Check that the argument type is not a valid type
			// Necessary to catch arguments which were generic in the declaration
			if( type?.SpecialType == SpecialType.None ) {
				// The current parameter type cannot be marked as [Constant]
				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.InvalidConstantType,
						location: argument.GetLocation(),
						messageArgs: type.TypeKind
					)
				);

				return;
			}

			// Check if the argument is a constant value
			if( !context.SemanticModel.GetConstantValue( argument.Expression ).HasValue ) {
				// Argument is not constant, so report it
				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.NonConstantPassedToConstantParameter,
						location: argument.GetLocation(),
						messageArgs: parameter.Name
					)
				);
			}
		}


		/// <summary>
		/// Check if the symbol has a specific attribute attached to it.
		/// </summary>
		/// <param name="symbol">The symbol to check for an attribute on</param>
		/// <param name="attributeSymbol">The symbol of the attribute</param>
		/// <returns>True if the attribute exists on the symbol, false otherwise</returns>
		public static bool HasAttribute(
			ISymbol symbol,
			ISymbol attributeSymbol
		) {
			if( attributeSymbol == null ) {
				return false;
			}

			return symbol.GetAttributes()
				.Any( attr => SymbolEqualityComparer.Default.Equals(
						attributeSymbol,
						attr.AttributeClass
					)
				);
		}
	}
}
