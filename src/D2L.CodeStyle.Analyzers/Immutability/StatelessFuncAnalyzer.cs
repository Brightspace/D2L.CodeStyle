using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;

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

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			context.RegisterSyntaxNodeAction(
				ctx => {
					if( ctx.Node is ObjectCreationExpressionSyntax expr ) {
						AnalyzeInvocation( ctx, expr );
					}
				},
				SyntaxKind.ObjectCreationExpression
			);
		}

		private void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			ObjectCreationExpressionSyntax syntax
		) {

			TypeSyntax type = syntax.Type;
			ISymbol symbol = context
				.SemanticModel
				.GetSymbolInfo( type ).Symbol;

			if( symbol == null ) {
				return;
			}

			if( !IsStatelessFunc( symbol ) ) {
				return;
			}

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
					break;

				// this is the case when the left hand side of the
				// lambda has parens
				// eg () => 1, (x, y) => x + y
				case SyntaxKind.ParenthesizedLambdaExpression:
					if( !CheckForClosures( context, argument ) ) {
						return;
					}
					break;

				// this is the case when the left hand side of the
				// lambda does not have parens
				// eg x => x + 1
				case SyntaxKind.SimpleLambdaExpression:
					if( !CheckForClosures( context, argument ) ) {
						return;
					}
					break;

				default:
					// should we do something else here because constructors
					// of D2L.StatelessFunc<T> and friends should always take
					// a func?
					return;
			}

			var diag = Diagnostic.Create(
				Diagnostics.StatelessFuncIsnt,
				argument.GetLocation()
			);

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

		private static bool CheckForClosures(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax expression
		) {

			DataFlowAnalysis dataFlow = context
				.SemanticModel
				.AnalyzeDataFlow( expression );

			ImmutableArray<ISymbol> captures = dataFlow.Captured;
			return captures.Length > 0;
		}

		private static bool IsStatelessFunc( ISymbol symbol ) {
			ImmutableHashSet<string> statelessFuncs = GetStatelessFuncTypes();

			string fullName = $"{ symbol.ContainingNamespace.Name }.{ symbol.MetadataName }";

			return statelessFuncs.Contains( fullName );
		}

		private static ImmutableHashSet<string> GetStatelessFuncTypes() {

			var builder = ImmutableArray.CreateBuilder<string>();
			builder.Add( "D2L.StatelessFunc`1" );
			builder.Add( "D2L.StatelessFunc`2" );
			builder.Add( "D2L.StatelessFunc`3" );
			builder.Add( "D2L.StatelessFunc`4" );
			builder.Add( "D2L.StatelessFunc`5" );
			builder.Add( "D2L.StatelessFunc`6" );
			ImmutableHashSet<string> typeNames = builder.ToImmutableHashSet();

			return typeNames;
		}
	}
}
