﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;
using System.Linq;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class StatelessFuncAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.StatelessFuncIsnt
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			ImmutableHashSet<ISymbol> statelessFuncs = GetStatelessFuncTypes( compilation );

			context.RegisterSyntaxNodeAction(
				ctx => {
						AnalyzeObjectCreationExpression(
							ctx,
							statelessFuncs
						);
				},
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeObjectCreationExpression(
			SyntaxNodeAnalysisContext context,
			ImmutableHashSet<ISymbol> statelessFuncs
		) {

			ObjectCreationExpressionSyntax syntax = context.Node as ObjectCreationExpressionSyntax;

			ISymbol symbol = context
				.SemanticModel
				.GetSymbolInfo( syntax ).Symbol;

			if( symbol == null ) {
				return;
			}

			if( !IsStatelessFunc( symbol, statelessFuncs ) ) {
				return;
			}

			Diagnostic diag;
			ExpressionSyntax argument = syntax.ArgumentList.Arguments[ 0 ].Expression;
			SyntaxKind kind = argument.Kind();
			switch( kind ) {

				// this is the case when a method reference is used
				// eg Func<string, int> func = int.Parse
				case SyntaxKind.SimpleMemberAccessExpression:
					if( IsStaticMemberAccess( context, argument ) ) {
						return;
					}

					// non-static member access means that state could
					// be used / held.
					// TODO: Look for [Immutable] on the member's type
					// to determine if the non-static member is safe
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"{ argument.ToString() } is not static"
					);
					break;

				// this is the case when the left hand side of the
				// lambda has parens
				// eg () => 1, (x, y) => x + y
				case SyntaxKind.ParenthesizedLambdaExpression:
				// this is the case when the left hand side of the
				// lambda does not have parens
				// eg x => x + 1
				case SyntaxKind.SimpleLambdaExpression:
					bool hasCaptures = TryGetCaptures(
						context,
						argument,
						out ImmutableArray<ISymbol> captures
					);
					if( !hasCaptures ) {
						return;
					}

					string captured = string.Join( ", ", captures.Select( c => c.Name ) );
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Captured variable(s): { captured }"
					);
					break;

				// this is the case where an expression is invoked,
				// which returns a Func<T>
				// eg ( () => { return () => 1 } )()
				case SyntaxKind.InvocationExpression:
					// we are rejecting this because it is tricky to
					// analyze properly, but also a bit ugly and should
					// never really be necessary
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Invocations are not allowed: { argument.ToString() }"
					);

					break;

				default:
					// we need StatelessFunc<T> to be ultra safe, so we'll
					// reject usages we do not understand yet
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Unable to determine safety of { argument.ToString() }. This is an unexpectes usage of StatelessFunc<T>"
					);

					break;
			}

			context.ReportDiagnostic( diag );
		}

		private static bool IsStaticMemberAccess(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax expression
		) {
			ISymbol symbol = context
				.SemanticModel
				.GetSymbolInfo( expression ).Symbol;

			if( symbol == null ) {
				return false;
			}

			return symbol.IsStatic;
		}

		private static bool TryGetCaptures(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax expression,
			out ImmutableArray<ISymbol> captures
		) {

			DataFlowAnalysis dataFlow = context
				.SemanticModel
				.AnalyzeDataFlow( expression );

			captures = dataFlow.Captured;
			return captures.Length > 0;
		}

		private static bool IsStatelessFunc(
			ISymbol symbol,
			ImmutableHashSet<ISymbol> statelessFuncs
		) {

			if( statelessFuncs.Contains( symbol ) ) {
				// we've found a definition that matches exactly with the symbol
				return true;
			}

			// Generics work a bit different, in that the symbol we have to work
			// with is not (eg StatelessFunc<int> ), but the list of symbols we're
			// checking against are the definitions, which are (eg
			// StatelessFunc<T> ) generic. So check the "parent" definition.
			return statelessFuncs.Contains( symbol.OriginalDefinition );
		}

		private static ImmutableHashSet<ISymbol> GetStatelessFuncTypes( Compilation compilation) {

			var builder = ImmutableHashSet.CreateBuilder<ISymbol>();

			var types = new string[] {
				"D2L.StatelessFunc`1",
				"D2L.StatelessFunc`2",
				"D2L.StatelessFunc`3",
				"D2L.StatelessFunc`4",
				"D2L.StatelessFunc`5",
				"D2L.StatelessFunc`6",
			};

			foreach( string typeName in types ) {
				INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName( typeName );

				if( typeSymbol == null ) {
					// These types are usually defined in another assembly,
					// ( usually ) and thus we have a bit of a circular
					// dependency. Allowing this lookup to return null
					// allow us to update the analyzer before the other
					// assembly, which should be safer
					continue;
				}

				foreach( IMethodSymbol ctor in typeSymbol.Constructors ) {
					builder.Add( ctor );
				}
			}

			return builder.ToImmutableHashSet();
		}
	}
}
