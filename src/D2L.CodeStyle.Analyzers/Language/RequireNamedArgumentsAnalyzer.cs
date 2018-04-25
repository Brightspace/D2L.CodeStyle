using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class RequireNamedArgumentsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.UseNamedArgsForInvocationWithLotsOfArgs
		);

		// TODO: shrink this number over time. Maybe 5 would be good?
		public const int TOO_MANY_UNNAMED_ARGS = 10;

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(
				AnalyzeInvocation,
				SyntaxKind.InvocationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext ctx
		) {
			var expr = (InvocationExpressionSyntax)ctx.Node;

			if ( expr.ArgumentList == null ) {
				return;
			}

			// Don't complain about single argument functions because they're
			// very likely to be understandable
			if ( expr.ArgumentList.Arguments.Count <= 1 ) {
				return;
			}

			var unnamedArgs = GetUnnamedArgs(
				ctx.SemanticModel,
				expr.ArgumentList
			).ToImmutableArray();

			if ( unnamedArgs.Length >= TOO_MANY_UNNAMED_ARGS ) {
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.UseNamedArgsForInvocationWithLotsOfArgs,
						location: expr.GetLocation()
					)
				);
			}

			// TODO: literal args should always be named

			// TODO: if there are duplicate typed args then they should be named
			// These will create a bit more cleanup. Fix should probably name
			// all the args instead to avoid craziness with overloading.
		}
		
		/// <summary>
		/// Get the arguments which are unnamed and not "params"
		/// </summary>
		private static IEnumerable<ArgumentSyntax> GetUnnamedArgs(
			SemanticModel model,
			ArgumentListSyntax args
		) {
			foreach( var arg in args.Arguments ) {
				// Ignore args that are already named. This will mean that args
				// can be partially named which is sometimes helpful (the named
				// args have to be at the end of the list though.)
				if ( arg.NameColon != null ) {
					continue;
				}

				var param = arg.DetermineParameter(
					model,

					// Don't map params arguments. It's okay that they are
					// unnamed. Some things like ImmutableArray.Create() could
					// take a large number of args and we don't want anything to
					// be named. Named params really suck so we may still
					// encourage it but APIs that take params and many other
					// args would suck anyway.
					allowParams: false
				);

				// We presumably can't name this param anyway
				if ( param == null ) {
					continue;
				}

				// IParameterSymbol.Name is documented to be possibly empty in
				// which case it is unnamed, so ignore it.
				if ( param.Name == "" ) {
					continue;
				}

				yield return arg;
			}
		}
	}
}