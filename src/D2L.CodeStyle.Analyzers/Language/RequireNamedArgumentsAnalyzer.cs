﻿using System;
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

		public const int TOO_MANY_UNNAMED_ARGS = 5;

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

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

				return;
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

				// Ignore args that already have names
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

				string psuedoName = GetPsuedoName( arg );

				if( psuedoName != null ) {
					bool matchesParamName = string.Equals(
						psuedoName,
						param.Name,
						StringComparison.OrdinalIgnoreCase
					);

					if( matchesParamName ) {
						continue;
					}
				}

				yield return new ArgParamBinding(
					position: idx,
					paramName: param.Name,
					syntax: arg
				);
			}
		}

		private static string GetPsuedoName( ArgumentSyntax arg ) {
			string ident = null;

			switch( arg.Expression ) {
				case IdentifierNameSyntax identArg:
					ident = identArg.Identifier.ValueText;
					break;
				case MemberAccessExpressionSyntax access:
					// Member access is left-associative, so we pick the
					// right -most ident, i.e. "foo.bar.baz" is equivalent to
					// (foo.bar).baz, and we will grab "baz".
					ident = access.Name.Identifier.ValueText;
					break;
			}

			if( ident == null ) {
				return null;
			}

			// Strip uninteresting prefixes off the identifier
			if ( ident.StartsWith( "m_" ) || ident.StartsWith( "s_" ) ) {
				// e.g. m_foo -> foo
				ident = ident.Substring( 2 );
			} else if ( ident[0] == '_' && ident.Length > 1 && ident[1] != '_') {
				// e.g. _foo -> foo
				ident = ident.Substring( 1 );
			}

			return ident;
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
