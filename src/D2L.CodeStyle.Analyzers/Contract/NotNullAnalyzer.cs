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

		public static void RegisterNotNullAnalyzer( CompilationStartAnalysisContext context ) {
			// For caching if a method has any not-null parameters, and the which ones are
			var notNullMethodCache = new ConcurrentDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>>();

			context.RegisterSyntaxNodeAction(
					ctx => AnalyzeInvocation(
							ctx,
							notNullMethodCache
						),
					SyntaxKind.InvocationExpression,
					SyntaxKind.ObjectCreationExpression
				);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache
		) {
			// It could be a method or constructor call, but there is no common interface or base
			// type, despite being very similar when coded, and analyzed
			var invocation = context.Node as InvocationExpressionSyntax;
			var construction = context.Node as ObjectCreationExpressionSyntax;

			if( invocation == null && construction == null ) {
				// A method isn't being invoked, so there's nothing to look at
				return;
			}

			var arguments = invocation?.ArgumentList.Arguments
				?? construction.ArgumentList.Arguments;
			if( arguments.Count == 0 ) {
				// We don't care about methods that take no arguments
				return;
			}

			IList<Tuple<ArgumentSyntax, IParameterSymbol>> notNullArguments;
			bool isNotNullMethod = TryGetNotNullArguments(
					context,
					(ExpressionSyntax)invocation ?? construction,
					arguments,
					notNullMethodCache,
					out notNullArguments
				);

			if( !isNotNullMethod ) {
				// The called method doesn't have any [NotNull] parameters, so there's nothing more to analyze
				return;
			}

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

		private static bool TryGetNotNullArguments(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax invocation,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			IDictionary<IMethodSymbol, ImmutableHashSet<IParameterSymbol>> notNullMethodCache,
			out IList<Tuple<ArgumentSyntax, IParameterSymbol>> notNullArguments
		) {
			IMethodSymbol invokedSymbol;
			if( !TryGetInvokedSymbol( context, invocation, out invokedSymbol ) ) {
				// There could either by multiple methods that match, in which case we don't know which we should
				// look at, or the method being called may not actually exist.
				notNullArguments = null;
				return false;
			}

			ImmutableArray<IParameterSymbol> parameters = invokedSymbol.Parameters;
			if( parameters.Length == 0 ) {
				// Method doesn't take any parameters so there's no need to look at arguments
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

			if( arguments.Count > parameters.Length
				// `params` converts multiple arguments into a single array parameter
				&& !parameters[parameters.Length - 1].IsParams
			) {
				// Something is weird, and we can't analyze this
				notNullArguments = null;
				return false;
			}

			notNullArguments = new List<Tuple<ArgumentSyntax, IParameterSymbol>>();
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
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax invocation,
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

		private static bool SymbolHasAttribute(
			ISymbol symbol,
			string attributeClassName,
			string expectedArgumentValue = null
		) {
			ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
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
