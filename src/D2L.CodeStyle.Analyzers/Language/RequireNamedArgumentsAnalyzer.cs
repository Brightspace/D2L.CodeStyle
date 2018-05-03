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

		public sealed class ArgParamBinding {
			public ArgParamBinding(
				int position,
				string paramName,
				ArgumentSyntax syntax
			) {
				Position = position;
				ParamName = paramName;
				Syntax = syntax;
			}

			public int Position { get; }
			public string ParamName { get; }
			public ArgumentSyntax Syntax { get; }
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.TooManyUnnamedArgs
		);

		// TODO: shrink this number over time. Maybe 5 would be good?
		public const int TOO_MANY_UNNAMED_ARGS = 31;

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeCallSyntax(
			SyntaxNodeAnalysisContext ctx
		) {
			var expr = (ExpressionSyntax)ctx.Node;
			var args = GetArgs( expr );

			if ( args == null ) {
				return;
			}

			// Don't complain about single argument functions because they're
			// very likely to be understandable
			if ( args.Arguments.Count <= 1 ) {
				return;
			}

			var unnamedArgs = GetUnnamedArgs(
				ctx.SemanticModel,
				args
			).ToImmutableArray();

			if ( unnamedArgs.Length >= TOO_MANY_UNNAMED_ARGS ) {
				// Pass the names and positions for each unnamed arg to the
				// codefix.
				var fixerContext = unnamedArgs.ToImmutableDictionary(
					keySelector: binding => binding.Position.ToString(),
					elementSelector: binding => binding.ParamName
				);

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.TooManyUnnamedArgs,
						location: expr.GetLocation(),
						properties: fixerContext
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
		private static IEnumerable<ArgParamBinding> GetUnnamedArgs(
			SemanticModel model,
			ArgumentListSyntax args
		) {
			for( var idx = 0; idx < args.Arguments.Count; idx++ ) {
				var arg = args.Arguments[idx];

				// Ignore args that are already named. This will mean that args
				// can be partially named which is sometimes helpful (the named
				// args have to be at the end of the list though.)
				if ( arg.NameColon != null ) {
					// We can stop as soon as we see one; named arguments have
					// to be at the end of the arguments so the rest will be
					// named too.
					yield break;
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

				// Not sure if this can happen but it'd be hard to name this
				// param so ignore it.
				if ( param == null ) {
					continue;
				}

				// IParameterSymbol.Name is documented to be possibly empty in
				// which case it is "unnamed", so ignore it.
				if ( param.Name == "" ) {
					continue;
				}

				yield return new ArgParamBinding(
					position: idx,
					paramName: param.Name,
					syntax: arg
				);
			}
		}

		// Not an extension method because there may be more cases (e.g. in the
		// future) and if more than this fix + its analyzer used this logic
		// there could be undesirable coupling if we handled more cases.
		internal static ArgumentListSyntax GetArgs( SyntaxNode syntax ) {
			switch( syntax ) {
				case InvocationExpressionSyntax invocation:
					return invocation.ArgumentList;
				case ObjectCreationExpressionSyntax objectCreation:
					return objectCreation.ArgumentList;
				default:
					return null;
			}
		}
	}
}
