using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Contract {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NotNullAnalyzer : DiagnosticAnalyzer {

		private const string NotNullAttribute = "D2L.CodeStyle.Annotations.Contract.NotNullAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.NullPassedToNotNullParameter );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterNotNullAnalyzer );
		}

		public static void RegisterNotNullAnalyzer( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
					AnalyzeUsage,
					SyntaxKind.Argument // TODO: Change this back to SyntaxKind.InvocationExpression?
				);
		}

		private static void AnalyzeUsage(
			SyntaxNodeAnalysisContext context
		) {
			var argument = context.Node as ArgumentSyntax;
			if( argument == null ) {
				return;
			}

			var argumentList = argument.Parent as ArgumentListSyntax;
			if( argumentList == null ) {
				return;
			}

			SeparatedSyntaxList<ArgumentSyntax> separatedSyntaxList = argumentList.Arguments;
			int argumentIndex = separatedSyntaxList.IndexOf( argument );

			var invocation = argumentList.Parent as InvocationExpressionSyntax;
			if( invocation == null ) {
				return;
			}

			var memberSymbol = context.SemanticModel.GetSymbolInfo( invocation.Expression ).Symbol as IMethodSymbol;
			if( memberSymbol == null ) {
				return;
			}

			ImmutableArray<IParameterSymbol> methodParameters = memberSymbol.Parameters;
			if( methodParameters.Length == 0 || methodParameters.Length < argumentIndex + 1 ) {
				// Make sure that the value being passed actually maps to a parameter
				return;
			}

			IParameterSymbol methodParameter = methodParameters[argumentIndex];
			bool hasNotNullAttribute = methodParameter.GetAttributes()
				.Any( x => x.ToString() == NotNullAttribute );
			if( !hasNotNullAttribute ) {
				return;
			}

			var literalValue = argument.Expression as LiteralExpressionSyntax;
			if( literalValue != null ) {
				if( literalValue.Token.Text != "null" ) {
					return;
				}
			} else {
				var identifierName = argument.Expression as IdentifierNameSyntax;
				if( identifierName == null ) {
					return;
				}

				var invocationSyntaxTree = invocation.SyntaxTree;
				SemanticModel semanticModel = context.Compilation.GetSemanticModel( invocationSyntaxTree );
				SymbolInfo symbolInfo = semanticModel.GetSymbolInfo( identifierName );
				ISymbol symbol = symbolInfo.Symbol;

				var methodDeclaration = GetAncestorNodeOfType<BaseMethodDeclarationSyntax>( invocation.Parent );
				if( methodDeclaration == null ) {
					return; // Likely part of a class declaration, which can't be checked anywhere near the same way
				}

				// Analyze the flow of data before this call took place
				DataFlowAnalysis dataFlowAnalysis = semanticModel.AnalyzeDataFlow( methodDeclaration.Body );
				ImmutableArray<ISymbol> variablesDeclared = dataFlowAnalysis.VariablesDeclared;

				bool variableIsLocal = variablesDeclared.Contains( symbol );
				if( !variableIsLocal ) {
					// It would be nigh-impossible to determine whether a non-null value is assigned to a non-local variable
					return;
				}

				ImmutableArray<ISymbol> alwaysAssigned = dataFlowAnalysis.AlwaysAssigned;
				bool variableIsAssigned = alwaysAssigned.Contains( symbol );
				if( variableIsAssigned ) { // If it's not always assigned, fall through and show an error
					int invocationStart = invocation.SpanStart;

					// Limit the search to between the method start & when the call under analysis is being invoked
					TextSpan searchLimits = new TextSpan( 0, invocationStart );

					IEnumerable<SyntaxNode> descendantNodes = methodDeclaration.Body.DescendantNodes(
							searchLimits,
							node => ( node is ExpressionSyntax || node is StatementSyntax )
						).Where(
							node => node is LocalDeclarationStatementSyntax
								|| node is AssignmentExpressionSyntax
						).ToArray();

					// Find out if it was assigned when it was declared
					VariableDeclaratorSyntax declarator = descendantNodes
						.OfType<LocalDeclarationStatementSyntax>()
						.SelectMany( x => x.Declaration.Variables )
						.SingleOrDefault( v => v.Identifier.Text == identifierName.Identifier.Text );

					if( declarator == null ) {
						return;
					}

					// If it was assigned a value at declaration, look at the value
					if( declarator.Initializer != null ) {
						if( IsNotNullValue( declarator.Initializer.Value )) {
							return;
						}
					}

					IEnumerable<ExpressionSyntax> assignmentValues = descendantNodes
						.OfType<AssignmentExpressionSyntax>()
						.Where(
							x => x.Left is IdentifierNameSyntax
								 && ( (IdentifierNameSyntax)x.Left ).Identifier.Text == identifierName.Identifier.Text
						).Select( x => x.Right );

					// TODO: Can this be modified to determine if a variable has null reassigned to it?
					if( assignmentValues.Any( IsNotNullValue ) ) {
						return;
					}
				}
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.NullPassedToNotNullParameter,
					argument.GetLocation()
				);
			context.ReportDiagnostic( diagnostic );
		}

		private static T GetAncestorNodeOfType<T>(
			SyntaxNode node
		) where T : SyntaxNode {
			while( !( node is T ) && node != null ) {
				node = node.Parent;
			}
			return node as T;
		}

		private static bool IsNotNullValue( ExpressionSyntax exp ) {
			return !( exp is LiteralExpressionSyntax ) // Not a literal expression, like `7`, or `"some string"`
				|| ((LiteralExpressionSyntax)exp).Token.Text != "null";
		}
	}
}
