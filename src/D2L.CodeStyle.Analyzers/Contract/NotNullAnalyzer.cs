﻿using System;
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
					AnalyzeInvocation,
					SyntaxKind.InvocationExpression
				);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context
		) {
			var invocation = context.Node as InvocationExpressionSyntax;
			if( invocation == null ) {
				return;
			}

			var memberSymbol = context.SemanticModel.GetSymbolInfo( invocation.Expression ).Symbol as IMethodSymbol;
			if( memberSymbol == null ) {
				return;
			}

			ImmutableArray<IParameterSymbol> parameters = memberSymbol.Parameters;
			if( parameters.Length == 0 ) {
				// We don't care about methods that take no arguments
				return;
			}

			var arguments = invocation.ArgumentList.Arguments;
			if( arguments.Count < parameters.Length ) {
				// Something is weird, and we can't analyze this
				return;
			}

			IList<ArgumentSyntax> notNullArguments = new List<ArgumentSyntax>();
			for( int i = 0; i < parameters.Length; i++ ) {
				IParameterSymbol param = parameters[i];
				ImmutableArray<AttributeData> attributes = param.GetAttributes();
				if( attributes.Length > 0
					&& attributes.Any( x => x.AttributeClass.ToString() == NotNullAttribute )
				) {
					notNullArguments.Add( arguments[i] );
				}
			}

			if( notNullArguments.Count == 0 ) {
				return;
			}

			// Using Lazy<T> so we don't do extra work if no arguments can be analyzed, or they don't need these values
			// They're also based on the invocation, not the argument, so only need to be fetched once
			var methodDeclaration = new Lazy<BaseMethodDeclarationSyntax>(
					() => GetAncestorNodeOfType<BaseMethodDeclarationSyntax>( invocation.Parent )
				);
			var semanticModel = new Lazy<SemanticModel>(
					() => context.Compilation.GetSemanticModel( invocation.SyntaxTree )
				);
			var dataFlowAnalysis = new Lazy<DataFlowAnalysis>(
					() => semanticModel.Value.AnalyzeDataFlow( methodDeclaration.Value.Body )
				);

			// Start analyzing the arguments
			foreach( ArgumentSyntax argument in notNullArguments ) {
				if( TryAnalyzeLiteralValueArgument( context, argument ) ) {
					continue;
				}

				var argumentIdentifierName = argument.Expression as IdentifierNameSyntax;
				if( argumentIdentifierName == null ) {
					continue;
				}

				// Now that we know it's not a literal value, and is an identifier name, these checks become relevant.
				if( methodDeclaration.Value == null ) {
					// Likely part of a class declaration, which requires very different work to determine if the
					// argument was ever assigned a value. However, other arguments may still be analyzed in simpler
					// ways, so don't entirely exit yet
					continue;
				}

				AnalyzeArgument(
						context,
						invocation,
						argument,
						argumentIdentifierName,
						methodDeclaration.Value,
						dataFlowAnalysis.Value,
						semanticModel.Value
					);
			}
		}

		private static bool TryAnalyzeLiteralValueArgument(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument
		) {
			var literalValue = argument.Expression as LiteralExpressionSyntax;
			if( literalValue == null ) {
				// It's not a literal value, allow other analyses to continue
				return false;
			}

			if( literalValue.Token.Text == "null" ) {
				MarkDiagnosticError( context, argument );
			}

			return true;
		}

		private static void AnalyzeArgument(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			ArgumentSyntax argument,
			IdentifierNameSyntax argumentIdentifierName,
			BaseMethodDeclarationSyntax methodDeclaration,
			DataFlowAnalysis dataFlowAnalysis,
			SemanticModel semanticModel
		) {
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo( argumentIdentifierName );
			ISymbol symbol = symbolInfo.Symbol;

			// Analyze the flow of data before this call took place
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
					.SingleOrDefault( v => v.Identifier.Text == argumentIdentifierName.Identifier.Text );

				if( declarator == null ) {
					return;
				}

				// If it was assigned a value at declaration, look at the value
				if( declarator.Initializer != null ) {
					if( IsNotNullValue( declarator.Initializer.Value ) ) {
						return;
					}
				}

				IEnumerable<ExpressionSyntax> assignmentValues = descendantNodes
					.OfType<AssignmentExpressionSyntax>()
					.Where(
						x => x.Left is IdentifierNameSyntax
							&& ( (IdentifierNameSyntax)x.Left ).Identifier.Text == argumentIdentifierName.Identifier.Text
					).Select( x => x.Right );

				// TODO: Can this be modified to determine if a variable has null reassigned to it?
				if( assignmentValues.Any( IsNotNullValue ) ) {
					return;
				}
			}

			MarkDiagnosticError( context, argument );
		}

		private static void MarkDiagnosticError(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument
		) {
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
