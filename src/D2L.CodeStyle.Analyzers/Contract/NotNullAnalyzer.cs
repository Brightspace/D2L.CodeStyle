using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Contract {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NotNullAnalyzer : DiagnosticAnalyzer {

		private const string Namespace = "D2L.CodeStyle.Annotations.Contract.";
		private const string NotNullAttribute = Namespace + "NotNullAttribute";
		private const string NotNullTypeAttribute = Namespace + "NotNullWhenParameterAttribute";
		private const string AllowNullAttribute = Namespace + "AllowNullAttribute";
		private const string AlwaysAssignedValueAttribute = Namespace + "AlwaysAssignedValueAttribute";
		private const string IgnoreNotNullErrorsAttribute = Namespace + "IgnoreNotNullErrorsAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.NullPassedToNotNullParameter );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterNotNullAnalyzer );
		}

		public static void RegisterNotNullAnalyzer( CompilationStartAnalysisContext context ) {
			// For caching if a method has any not-null parameters, and the which ones are
			var notNullMethodCache = new ConcurrentDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>>();
			// For caching is a type has [NotNullWhenParameter] applied
			var notNullTypeCache = new ConcurrentDictionary<ITypeSymbol, bool>();

			context.RegisterSyntaxNodeAction(
					ctx => AnalyzeInvocation(
							ctx,
							notNullMethodCache,
							notNullTypeCache
						),
					SyntaxKind.InvocationExpression
				);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache,
			IDictionary<ITypeSymbol, bool> notNullTypeCache
		) {
			SemanticModel semanticModel = context.SemanticModel;
			var invocation = context.Node as InvocationExpressionSyntax;
			if( invocation == null ) {
				return;
			}

			// Check to see if errors are being ignored
			var methodDeclaration = GetAncestorNodeOfType<BaseMethodDeclarationSyntax>( invocation );
			if( methodDeclaration != null ) {
				IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol( methodDeclaration );
				if( SymbolHasAttribute( methodSymbol, IgnoreNotNullErrorsAttribute ) ) {
					return;
				};
			}
			var classDeclaration = GetAncestorNodeOfType<TypeDeclarationSyntax>( invocation );
			if( classDeclaration != null ) {
				INamedTypeSymbol classSymbol = semanticModel.GetDeclaredSymbol( classDeclaration );
				if( SymbolHasAttribute( classSymbol, IgnoreNotNullErrorsAttribute ) ) {
					return;
				}
			}

			var arguments = invocation.ArgumentList.Arguments;
			if( arguments.Count == 0 ) {
				// We don't care about methods that take no arguments
				return;
			}

			IList<Tuple<ArgumentSyntax, IParameterSymbol>> notNullArguments;
			bool isNotNullMethod = TryGetNotNullArguments(
					context,
					invocation,
					arguments,
					notNullMethodCache,
					notNullTypeCache,
					out notNullArguments
				);

			if( !isNotNullMethod ) {
				return;
			}

			// Using Lazy<T> so we don't do extra work if no arguments need to be analyzed, and only
			// needs to be processed once per invocation, rather than per argument
			var localVariables = new Lazy<ImmutableArray<ISymbol>>(
					() => semanticModel.AnalyzeDataFlow( methodDeclaration.Body ).VariablesDeclared
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
					// Method call, coallesce, etc., which we can't guarantee a good analysis on
					continue;
				}

				if( methodDeclaration == null ) {
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
						methodDeclaration,
						localVariables.Value,
						semanticModel
					);
			}
		}

		private static bool TryGetNotNullArguments(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache,
			IDictionary<ITypeSymbol, bool> notNullTypeCache,
			out IList<Tuple<ArgumentSyntax, IParameterSymbol>> notNullArguments
		) {
			IMethodSymbol invokedSymbol;
			if( !TryGetInvokedSymbol( context, invocation, arguments, out invokedSymbol ) ) {
				notNullArguments = null;
				return false;
			}

			ImmutableHashSet<IParameterSymbol> notNullParameterCache;
			if( notNullMethodCache.TryGetValue( invokedSymbol, out notNullParameterCache )
				&& notNullParameterCache == null
			) {
				// We've examined it before, and nothing needs to be not null
				notNullArguments = null;
				return false;
			}

			ImmutableArray<IParameterSymbol> parameters = invokedSymbol.Parameters;
			if( arguments.Count > parameters.Length ) {
				// Something is weird, and we can't analyze this
				notNullArguments = null;
				return false;
			}

			notNullArguments = new List<Tuple<ArgumentSyntax, IParameterSymbol>>();
			for( int i = 0; i < arguments.Count; i++ ) {
				ArgumentSyntax argument = arguments[i];
				IParameterSymbol param;

				if( !TryGetParameter( argument, parameters, i, out param ) ) {
					continue;
				}

				if( notNullParameterCache != null ) {
					if( notNullParameterCache.Contains( param ) ) {
						notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
					}
					continue;
				}

				// Check if the parameter type is not allowed to be null
				var paramAttributes = new Lazy<ImmutableArray<AttributeData>>( param.GetAttributes );

				if( IsNotNullType( param.Type, notNullTypeCache ) ) {
					if( !AttributeListContains( paramAttributes.Value, AllowNullAttribute ) ) {
						notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
					}

					// Ignore any [NotNull] as either the type makes it implicit, or [AllowNull] overrides it
					continue;
				}

				// Check if the parameter has [NotNull]
				if( AttributeListContains( paramAttributes.Value, NotNullAttribute ) ) {
					notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
				}
			}

			bool isNotNullMethod = notNullArguments.Count > 0;
			notNullMethodCache[invokedSymbol] = notNullArguments
				.Select( x => x.Item2 )
				.ToImmutableHashSet();
			return isNotNullMethod;
		}

		private static bool TryGetParameter(
			ArgumentSyntax argument,
			ImmutableArray<IParameterSymbol> parameters,
			int argumentIndex,
			out IParameterSymbol parameter
		) {
			// Not a named parameter
			if( argument.NameColon == null ) { // Regular order-based
				parameter = parameters[argumentIndex];
				return true;
			}

			// While parameters with @ are allowed in C#, the compiler strips it from the parameter
			// name, but not from the named argument
			string argumentName = argument.NameColon.Name.ToString().TrimStart( '@' );
			parameter = parameters.SingleOrDefault( p => p.Name == argumentName );

			return parameter != null;
		}

		private static bool TryGetInvokedSymbol(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			out IMethodSymbol invokedSymbol
		) {
			SymbolInfo invokedSymbolInfo = context.SemanticModel.GetSymbolInfo( invocation );

			// The simple case, where there's no ambiguity
			invokedSymbol = invokedSymbolInfo.Symbol as IMethodSymbol;
			if( invokedSymbol != null ) {
				return true;
			}

			// Default parameter values might result in a single candidate, but doesn't guarantee
			// resolution to a method symbol
			if( invokedSymbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure
				&& invokedSymbolInfo.CandidateSymbols.Length == 1
			) {
				invokedSymbol = invokedSymbolInfo.CandidateSymbols[0] as IMethodSymbol;
			}

			return invokedSymbol != null;
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
			ImmutableArray<ISymbol> localVariables,
			SemanticModel semanticModel
		) {
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo( argumentIdentifierName );
			ISymbol symbol = symbolInfo.Symbol;

			if( !localVariables.Contains( symbol ) ) {
				// It would be nigh-impossible to determine whether a non-null value is assigned to a non-local variable
				return;
			}

			// Find the innermost block in which it was declared, and do our analysis in there
			BlockSyntax declaringBlock = GetAncestorNodeOfType<BlockSyntax>( invocation );
			while( declaringBlock != methodDeclaration.Body ) {
				DataFlowAnalysis innerAnalysis = semanticModel.AnalyzeDataFlow( declaringBlock );
				if( innerAnalysis.VariablesDeclared.Contains( symbol ) ) {
					break;
				}
				declaringBlock = GetAncestorNodeOfType<BlockSyntax>( declaringBlock );
			}

			bool variableIsAssigned = VariableIsAlwaysAssignedAfterDeclaration(
					semanticModel,
					declaringBlock,
					symbol
				);

			if( variableIsAssigned ) { // If it's not always assigned, fall through and show an error
				// Limit the search to between the block start & when the call under analysis is being invoked
				TextSpan searchLimits = new TextSpan(
						declaringBlock.SpanStart,
						invocation.SpanStart - declaringBlock.SpanStart
					);
				IEnumerable<SyntaxNode> descendantNodes = declaringBlock.DescendantNodes( searchLimits )
					.Where(
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

			MarkDiagnosticError( context, argument, parameter );
		}

		private static bool VariableIsAlwaysAssignedAfterDeclaration(
			SemanticModel semanticModel,
			BlockSyntax declaringBlock,
			ISymbol variable
		) {
			VariableDeclaratorSyntax declarator = declaringBlock
				.ChildNodes()
				.OfType<LocalDeclarationStatementSyntax>()
				.SelectMany( x => x.Declaration.Variables )
				.FirstOrDefault( x => x.Identifier.Text == variable.Name );
			if( declarator == null ) {
				// This can happen in delegates, `Func`s, or similar
				return true;
			}

			IEnumerable<SyntaxNode> nodesToRemove = new SyntaxNode[0];

			// Look at the delcaration, and potential assignment there
			if( declarator.Initializer != null ) {
				if( IsNotNullValue( declarator.Initializer.Value ) ) {
					return true;
				}
				nodesToRemove = new[] { declarator.Initializer };
			} else if( VariableIsAlwaysAssigned( declaringBlock, declarator, semanticModel ) ) {
				return true;
			}

			// See if it has a "AlwaysAssignedValue" attribute
			var methodDeclaration = GetAncestorNodeOfType<BaseMethodDeclarationSyntax>( declaringBlock );
			IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol( methodDeclaration );
			bool hasAlwaysAssignedAttribute = SymbolHasAttribute(
					methodSymbol,
					AlwaysAssignedValueAttribute,
					variable.Name
				);

			if( hasAlwaysAssignedAttribute ) {
				return true;
			}

			// Find nodes between beginning of block and the variable's declaration
			nodesToRemove = declaringBlock.ChildNodes()
				.Where( x => x.Span.End < declarator.SpanStart )
				.Concat( nodesToRemove );

			// Find child blocks that potentially do a return, as these will stop it from "always" being assigned a value
			ControlFlowAnalysis controlFlowAnalysis = semanticModel.AnalyzeControlFlow( declaringBlock );
			nodesToRemove = controlFlowAnalysis.ExitPoints
				.Select( GetAncestorNodeOfType<BlockSyntax> )
				.Distinct()
				.Where( x => !x.Equals( declaringBlock ) )
				.SelectMany( x => x.ChildNodes() )
				.Concat( nodesToRemove );

			// Remove the nodes, recompile, and check the status
			var compilationUnit = GetAncestorNodeOfType<CompilationUnitSyntax>( declaringBlock );
			compilationUnit = compilationUnit.RemoveNodes(
						nodesToRemove,
						SyntaxRemoveOptions.KeepDirectives
					);

			return VariableIsAlwaysAssignedInRecompiledCode(
					declaringBlock,
					variable,
					compilationUnit
				);
		}

		private static bool VariableIsAlwaysAssignedInRecompiledCode(
			BlockSyntax declaringBlock,
			ISymbol variable,
			CompilationUnitSyntax compilationUnit
		) {
			var compilation = CSharpCompilation.Create( "ThrowAwayCompilation" )
				.AddSyntaxTrees( compilationUnit.SyntaxTree ); // Re-find the parent block in the new compilation
			var parentBlock = compilationUnit.FindNode( declaringBlock.OpenBraceToken.Span );
			declaringBlock = parentBlock.DescendantNodes().OfType<BlockSyntax>()
				.FirstOrDefault( x => x.SpanStart == declaringBlock.SpanStart ) ?? (BlockSyntax)parentBlock;

			// Get the new declaration for the variable
			VariableDeclaratorSyntax declarator = declaringBlock.Statements
				.OfType<LocalDeclarationStatementSyntax>()
				.SelectMany( x => x.Declaration.Variables )
				.First( x => x.Identifier.Text == variable.Name );

			// Determine if the variable is assigned again
			SemanticModel semanticModel = compilation.GetSemanticModel( declaringBlock.SyntaxTree );
			return VariableIsAlwaysAssigned( declaringBlock, declarator, semanticModel );
		}

		private static bool VariableIsAlwaysAssigned(
			BlockSyntax declaringBlock,
			VariableDeclaratorSyntax declarator,
			SemanticModel semanticModel
		) {
			ISymbol symbol = semanticModel.GetDeclaredSymbol( declarator );
			bool variableIsAssigned = semanticModel.AnalyzeDataFlow(
					GetAncestorNodeOfType<LocalDeclarationStatementSyntax>( declarator ),
					declaringBlock.ChildNodes().Last()
				).AlwaysAssigned.Select( x => x.Name )
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
			return node.Parent.FirstAncestorOrSelf<T>();
		}

		private static bool IsNotNullValue( ExpressionSyntax exp ) {
			return !( exp is LiteralExpressionSyntax ) // Not a literal expression, like `7`, or `"some string"`
				|| ((LiteralExpressionSyntax)exp).Token.Text != "null";
		}

		private static bool IsNotNullType(
			ITypeSymbol paramType,
			IDictionary<ITypeSymbol, bool> notNullTypeCache
		) {
			bool isNotNull;
			if( notNullTypeCache.TryGetValue( paramType, out isNotNull ) ) {
				return isNotNull;
			}

			isNotNull = SymbolHasAttribute( paramType, NotNullTypeAttribute );
			notNullTypeCache[paramType] = isNotNull;
			return isNotNull;
		}

		private static bool SymbolHasAttribute(
			ISymbol symbol,
			string attributeClassName,
			string expectedArgumentValue = null
		) {
			return AttributeListContains(
					symbol.GetAttributes(),
					attributeClassName,
					expectedArgumentValue
				);
		}

		private static bool AttributeListContains(
			ImmutableArray<AttributeData> attributes,
			string attributeClassName,
			string expectedArgumentValue = null
		) {
			return attributes.Length > 0
				&& attributes.Any(
					x => x.AttributeClass.ToString() == attributeClassName
						&& (
							expectedArgumentValue == null 
							|| (
								x.ConstructorArguments.Length >= 1
								&& x.ConstructorArguments[0].Value.ToString() == expectedArgumentValue
							)
						)
				);
		}

	}
}
