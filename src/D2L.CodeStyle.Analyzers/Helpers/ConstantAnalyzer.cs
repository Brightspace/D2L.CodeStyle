using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Helpers {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ConstantAnalyzer : DiagnosticAnalyzer {
		private const string AttributeName = "D2L.CodeStyle.Annotations.Contract.ConstantAttribute";

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
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeParameters(
					ctx,
					(ParameterSyntax)ctx.Node
				),
				SyntaxKind.Parameter
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeArguments(
					ctx,
					(ArgumentSyntax)ctx.Node
				),
				SyntaxKind.Argument
			);
		}

		private static void AnalyzeParameters(
			SyntaxNodeAnalysisContext context,
			ParameterSyntax parameterSyntax
		) {
			var parameter = context.SemanticModel.GetDeclaredSymbol( parameterSyntax );

			// Parameter is somehow null, so do nothing
			if( parameter == null ) {
				return;
			}

			// Parameter is not [Constant], so do nothing
			if( !parameter.HasAttribute( context.Compilation, AttributeName ) ) {
				return;
			}

			var type = parameter.Type;

			// If the parameter is a valid type, there are no issues
			if( type.SpecialType != SpecialType.None
			) {
				return;
			}

			// If the parameter is a generic type, there aren't issues yet...
			// There may be issues when the method is being called, but
			// those are handled in the argument analyzer
			if( type.Kind != SymbolKind.NamedType
			) {
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
			ArgumentSyntax argument ) {
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
			if( !parameter.HasAttribute( context.Compilation, AttributeName ) ) {
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
	}
}
