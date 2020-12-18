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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.NonConstantPassedToConstantParameter
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
				ctx => AnalyzeInvocation(
					ctx,
					(InvocationExpressionSyntax)ctx.Node
				),
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeObjectCreation(
					ctx,
					(ObjectCreationExpressionSyntax)ctx.Node
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation
		) {
			AnalyzeInvocationLikeThing(
				context,
				invocation.ArgumentList
			);
		}

		private static void AnalyzeObjectCreation(
			SyntaxNodeAnalysisContext context,
			ObjectCreationExpressionSyntax construction
		) {
			AnalyzeInvocationLikeThing(
				context,
				construction.ArgumentList
			);
		}


		private static void AnalyzeInvocationLikeThing(
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
					allowParams: true );

				// Parameter is somehow null, so do nothing
				if( parameter == null ) {
					continue;
				}

				// Parameter is not [Constant], so do nothing
				if( !parameter.HasAttribute( "D2L.CodeStyle.Annotations.Contract.ConstantAttribute" ) ) {
					continue;
				}

				// Check if the argument is a constant value
				if( !context.SemanticModel.GetConstantValue( argument.Expression ).HasValue ) {

					// Argument is not constant, so report it
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.NonConstantPassedToConstantParameter,
						argument.GetLocation(),
						parameter.Name ) );
				}
			}
		}
	}
}
