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
					(ParameterListSyntax)ctx.Node
				),
				SyntaxKind.ParameterList
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeArguments(
					ctx,
					( (InvocationExpressionSyntax)ctx.Node ).ArgumentList
				),
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeArguments(
					ctx,
					( (ObjectCreationExpressionSyntax)ctx.Node ).ArgumentList
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeParameters(
			SyntaxNodeAnalysisContext context,
			ParameterListSyntax parameterList
		) {
			SeparatedSyntaxList<ParameterSyntax> parameters = parameterList.Parameters;
			foreach( ParameterSyntax parameterSyntax in parameters ) {
				// We need the symbol
				IParameterSymbol parameter = context.SemanticModel.GetDeclaredSymbol( parameterSyntax );

				// Parameter is somehow null, so do nothing
				if( parameter == null ) {
					continue;
				}

				// Parameter is not [Constant], so do nothing
				if( !parameter.HasAttribute( AttributeName ) ) {
					continue;
				}

				// Get the base type of the parameter
				ITypeSymbol type = parameter.Type;

				// Check that the parameter type is not a valid type,
				// and that it is not a type parameter
				if( type.SpecialType != SpecialType.None
				 || type.Kind != SymbolKind.NamedType ) {
					continue;
				}

				// This is marked as [Constant] and so you need to use a different type
				context.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.InvalidConstantType,
						location: parameterSyntax.GetLocation(),
						messageArgs: type.TypeKind
					)
				);
			}
		}

		private static void AnalyzeArguments(
			SyntaxNodeAnalysisContext context,
			ArgumentListSyntax argumentList ) {
			// If there are no arguments, nothing needs to be done
			if( argumentList == null ) {
				return;
			}

			// Get arguments from the list and iterate through them
			SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;
			foreach( var argument in arguments ) {
				// Get the associated parameter
				var parameter = argument.DetermineParameter(
					context.SemanticModel,
					allowParams: true
				);

				// Parameter is somehow null, so do nothing
				if( parameter == null ) {
					continue;
				}

				// Parameter is not [Constant], so do nothing
				if( !parameter.HasAttribute( AttributeName ) ) {
					continue;
				}

				// Get the base type of the argument
				var type = context.SemanticModel.GetTypeInfo( argument.Expression ).Type;

				// Check that the argument type is not a valid type
				if( type?.SpecialType == SpecialType.None ) {
					// This is marked as [Constant] and so you need to use a different type
					context.ReportDiagnostic(
						Diagnostic.Create(
							descriptor: Diagnostics.InvalidConstantType,
							location: argument.GetLocation(),
							messageArgs: type.TypeKind
						)
					);

					continue;
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
}
