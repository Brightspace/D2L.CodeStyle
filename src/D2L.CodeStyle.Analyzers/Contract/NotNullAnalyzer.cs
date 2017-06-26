using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ArgumentSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using LiteralExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax;
using LocalDeclarationStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;

namespace D2L.CodeStyle.Analyzers.Contract {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NotNullAnalyzer : DiagnosticAnalyzer {

		private const string NotNullAttribute = "D2L.CodeStyle.Annotations.Contract.NotNullAttribute",
			NotNullTypeAttribute = "D2L.CodeStyle.Annotations.Contract.NotNullWhenParameterAttribute",
			AllowNullAttribute = "D2L.CodeStyle.Annotations.Contract.AllowNullAttribute";

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

			var arguments = invocation.ArgumentList.Arguments;
			if( arguments.Count == 0 ) {
				// We don't care about methods that take no arguments
				return;
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo( invocation );
			var memberSymbol = symbolInfo.Symbol as IMethodSymbol;
			if( memberSymbol == null ) {
				return;
			}

			ImmutableArray<IParameterSymbol> parameters = memberSymbol.Parameters;
			if( arguments.Count < parameters.Length ) {
				// Something is weird, and we can't analyze this
				return;
			}

			IList<Tuple<ArgumentSyntax, IParameterSymbol>> notNullArguments = new List<Tuple<ArgumentSyntax, IParameterSymbol>>();
			for( int i = 0; i < parameters.Length; i++ ) {
				ArgumentSyntax argument = arguments[i];
				IParameterSymbol param;

				if( argument.NameColon == null ) { // Regular order-based
					param = parameters[i];
				} else { // Named parameter
					// While parameters with @ are allowed in C#, the compiler strips it from the parameter
					// name, but not from the named argument
					string argumentName = argument.NameColon.Name.ToString().TrimStart( '@' );
					param = parameters.Single( p => p.Name == argumentName );
				}

				// Check if the parameter has [NotNull]
				ImmutableArray<AttributeData> paramAttributes = param.GetAttributes();
				if( paramAttributes.Length > 0
					&& paramAttributes.Any( x => x.AttributeClass.ToString() == NotNullAttribute )
				) {
					notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
					continue;
				}

				// Check if the parameter type is not allowed to be null
				// TODO: Is it worth caching which types allow null?
				ImmutableArray<AttributeData> typeAttributes = param.Type.GetAttributes();
				if( typeAttributes.Length > 0
					&& typeAttributes.Any( x => x.AttributeClass.ToString() == NotNullTypeAttribute )
					&& !paramAttributes.Any( x => x.AttributeClass.ToString() == AllowNullAttribute )
				) {
					notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
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
			foreach( Tuple<ArgumentSyntax, IParameterSymbol> tuple in notNullArguments ) {
				ArgumentSyntax argument = tuple.Item1;
				IParameterSymbol parameter = tuple.Item2;

				if( TryAnalyzeLiteralValueArgument( context, argument, parameter ) ) {
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
						parameter,
						argumentIdentifierName,
						methodDeclaration.Value,
						dataFlowAnalysis.Value,
						semanticModel.Value
					);
			}
		}

		private static bool TryAnalyzeLiteralValueArgument(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument,
			IParameterSymbol parameter
		) {
			var literalValue = argument.Expression as LiteralExpressionSyntax;
			if( literalValue == null ) {
				// It's not a literal value, allow other analyses to continue
				return false;
			}

			if( literalValue.Token.Text == "null" ) {
				MarkDiagnosticError( context, argument, parameter );
			}

			return true;
		}

		private static void AnalyzeArgument(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			ArgumentSyntax argument,
			IParameterSymbol parameter,
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

					variableIsAssigned = VariableIsAlwaysAssignedAfterDeclaration(
							methodDeclaration,
							declarator
						);
				}

				if( variableIsAssigned ) {
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
			}

			MarkDiagnosticError( context, argument, parameter );
		}

		// Used for determining if a variable that was assigned `null` at declaration is guaranteed to be
		// assigned another value later on
		private static bool VariableIsAlwaysAssignedAfterDeclaration(
			BaseMethodDeclarationSyntax methodDeclaration,
			VariableDeclaratorSyntax declarator
		) {
			// Pretend the assignment didn't exist, and see if it's still always assigned;
			// Have to recompile the tree after removing the assignment, unfortunately
			var compilationUnit = GetAncestorNodeOfType<CompilationUnitSyntax>( methodDeclaration )
				.RemoveNode(
					declarator.Initializer,
					SyntaxRemoveOptions.KeepDirectives
				);
			var compilation = CSharpCompilation.Create( "ThrowAwayCompilation" )
				.AddSyntaxTrees( compilationUnit.SyntaxTree );

			// Re-find the parent method in the new compilation
			var typeDeclaration = compilationUnit.FindNode( methodDeclaration.Span ) as TypeDeclarationSyntax;
			methodDeclaration = (BaseMethodDeclarationSyntax)typeDeclaration.ChildNodes()
				.First( x => x.SpanStart == methodDeclaration.SpanStart );

			// Get the new symbol for the variable
			declarator = methodDeclaration.DescendantNodes( declarator.Identifier.Span )
				.OfType<VariableDeclaratorSyntax>()
				.Single();

			SemanticModel semanticModel = compilation.GetSemanticModel( methodDeclaration.SyntaxTree );
			ISymbol symbol = semanticModel.GetDeclaredSymbol( declarator );

			// Determine if the variable is assigned again
			bool variableIsAssigned = semanticModel
				.AnalyzeDataFlow( methodDeclaration.Body )
				.AlwaysAssigned.Select( x => x.Name )
				.Contains( symbol.Name );
			return variableIsAssigned;
		}

		private static void MarkDiagnosticError(
			SyntaxNodeAnalysisContext context,
			ArgumentSyntax argument,
			IParameterSymbol parameter
		) {
			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.NullPassedToNotNullParameter,
					argument.GetLocation(),
					parameter.Name
				);
			context.ReportDiagnostic( diagnostic );
		}

		private static T GetAncestorNodeOfType<T>(
			SyntaxNode node
		) where T : SyntaxNode {
			return (T)node.FirstAncestorOrSelf<SyntaxNode>( x => x is T );
		}

		private static bool IsNotNullValue( ExpressionSyntax exp ) {
			return !( exp is LiteralExpressionSyntax ) // Not a literal expression, like `7`, or `"some string"`
				|| ((LiteralExpressionSyntax)exp).Token.Text != "null";
		}
	}
}
