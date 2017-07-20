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

namespace D2L.CodeStyle.Analyzers.Contract {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NotNullAnalyzer : DiagnosticAnalyzer {

		private const string Namespace = "D2L.CodeStyle.Annotations.Contract.";
		private const string NotNullAttribute = Namespace + "NotNullAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.NullPassedToNotNullParameter );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterNotNullAnalyzer );
		}

		public static void RegisterNotNullAnalyzer(
			CompilationStartAnalysisContext context
		) {
			// For caching if a method has any not-null parameters, and the which ones are
			var notNullMethodCache = new ConcurrentDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>>();

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation(
						ctx,
						(InvocationExpressionSyntax)ctx.Node,
						notNullMethodCache
					),
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeObjectCreation(
					ctx,
					(ObjectCreationExpressionSyntax)ctx.Node,
					notNullMethodCache
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache
		) {
			AnalyzeInvocationLikeThing(
				context,
				invocation.ArgumentList.Arguments,
				invocation,
				notNullMethodCache
			);
		}

		private static void AnalyzeObjectCreation(
			SyntaxNodeAnalysisContext context,
			ObjectCreationExpressionSyntax construction,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache
		) {
			AnalyzeInvocationLikeThing(
				context,
				construction.ArgumentList.Arguments,
				construction,
				notNullMethodCache
			);
		}

		/// <summary>
		/// Checks that some sort of method call respects [NotNull] annotations
		/// from the methods declaration.
		/// </summary>
		private static void AnalyzeInvocationLikeThing(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			ExpressionSyntax expression,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache
		) {
			if( arguments.Count == 0 ) {
				// We don't care about methods that take no arguments
				return;
			}

			var notNullArguments = GetNotNullArguments(
				context.SemanticModel,
				expression,
				arguments,
				notNullMethodCache
			);

			// Start analyzing the arguments
			foreach( Tuple<ArgumentSyntax, IParameterSymbol> tuple in notNullArguments ) {
				ArgumentSyntax argument = tuple.Item1;
				IParameterSymbol parameter = tuple.Item2;

				var literalExpression = argument.Expression as LiteralExpressionSyntax;
				if( literalExpression != null
					&& literalExpression.Token.Kind() == SyntaxKind.NullKeyword
				) {
					// It is a literal "null" keyword, so mark it as an error
					Diagnostic diagnostic = Diagnostic.Create(
							Diagnostics.NullPassedToNotNullParameter,
							argument.GetLocation(),
							parameter.Name
						);
					context.ReportDiagnostic( diagnostic );
				}
			}
		}

		private static IEnumerable<Tuple<ArgumentSyntax, IParameterSymbol>>  GetNotNullArguments(
			SemanticModel semanticModel,
			ExpressionSyntax invocation,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache
		) {
			IMethodSymbol invokedSymbol;
			if( !TryGetInvokedSymbol( semanticModel, invocation, out invokedSymbol ) ) {
				// There could either be multiple methods that match, in which case we don't know which we should
				// look at, or the method being called may not actually exist.
				return Enumerable.Empty<Tuple<ArgumentSyntax, IParameterSymbol>>();
			}

			ImmutableArray<IParameterSymbol> parameters = invokedSymbol.Parameters;
			if( parameters.Length == 0 ) {
				// Method doesn't take any parameters so there's no need to look at arguments
				return Enumerable.Empty<Tuple<ArgumentSyntax, IParameterSymbol>>();
			}

			ImmutableHashSet<IParameterSymbol> notNullParameterCache;
			if( notNullMethodCache.TryGetValue( invokedSymbol, out notNullParameterCache )
				&& notNullParameterCache.Count == 0
			) {
				// We've examined it before, and nothing needs to be not null
				return Enumerable.Empty<Tuple<ArgumentSyntax, IParameterSymbol>>();
			}

			if( arguments.Count > parameters.Length
				// `params` converts multiple arguments into a single array parameter
				&& !parameters[parameters.Length - 1].IsParams
			) {
				// Something is weird, and we can't analyze this
				return Enumerable.Empty<Tuple<ArgumentSyntax, IParameterSymbol>>();
			}

			var notNullArguments = new List<Tuple<ArgumentSyntax, IParameterSymbol>>();
			for( int i = 0; i < arguments.Count; i++ ) {
				ArgumentSyntax argument = arguments[i];
				IParameterSymbol param;

				if( !TryGetParameter( argument, parameters, i, out param ) ) {
					// If the parameter matching the argument can't be retrieved for whatever reason then
					// we can't examine the attributes on it
					continue;
				}

				if( notNullParameterCache != null ) {
					if( notNullParameterCache.Contains( param ) ) {
						notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
					}
					// We've previously cached whether or not this parameter has [NotNull], so
					// there's no need to examine it again
					continue;
				}

				// Check if the parameter has [NotNull]
				if( SymbolHasAttribute( param, NotNullAttribute ) ) {
					notNullArguments.Add( new Tuple<ArgumentSyntax, IParameterSymbol>( arguments[i], param ) );
				}
			}

			if( notNullParameterCache == null ) {
				// Cache the values, if we didn't just pull it from the cache
				notNullMethodCache[invokedSymbol] = notNullArguments
					.Select( x => x.Item2 )
					.ToImmutableHashSet();
			}

			return notNullArguments;
		}

		private static bool TryGetParameter(
			ArgumentSyntax argument,
			ImmutableArray<IParameterSymbol> parameters,
			int argumentIndex,
			out IParameterSymbol parameter
		) {
			// Not a named parameter
			if( argument.NameColon == null ) { // Regular order-based
				if( argumentIndex >= parameters.Length ) {
					// `params` parameter, where the remaining arguments all apply to the last parameter
					parameter = parameters[parameters.Length - 1];
					return true;
				}
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
			SemanticModel semanticModel,
			ExpressionSyntax invocation,
			out IMethodSymbol invokedSymbol
		) {
			SymbolInfo invokedSymbolInfo = semanticModel.GetSymbolInfo( invocation );

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

		private static bool SymbolHasAttribute(
			ISymbol symbol,
			string attributeClassName
		) {
			ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
			bool hasExpectedAttribute = attributes.Length > 0
				&& attributes.Any(
					x => x.AttributeClass.GetFullTypeName() == attributeClassName
				);
			return hasExpectedAttribute;
		}

	}
}
