using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class LikelyArgumentMismatchAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.LikelyArgumentMismatch
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation(
					ctx,
					(InvocationExpressionSyntax)ctx.Node
				),
				SyntaxKind.InvocationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax expr
		) {
			if( expr.ArgumentList == null ) {
				return;
			}

			if( expr.ArgumentList.Arguments.Count <= 1 ) {
				return;
			}

			// get bipartite matching of the source
			ImmutableArray<Edge> sourceMatching = MatchArguments(
				context.SemanticModel,
				expr.ArgumentList.Arguments
			).ToImmutableArray();

			if( sourceMatching.Length <= 1 ) {
				return;
			}

			// this is currently n!, so, bail out for long lists
			// (which may be where its needed most!)
			if( sourceMatching.Length > 6 ) {
				return;
			}

			ImmutableArray<Edge> bestMatching = GenerateBestMatching( sourceMatching );

			ImmutableDictionary<int, Edge> sourceEdges = sourceMatching
				.ToImmutableDictionary( x => x.ArgId, x => x );
			foreach( var edge in bestMatching ) {
				Edge sourceEdge = sourceEdges[ edge.ArgId ];
				if( edge.Param != sourceEdge.Param ) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.LikelyArgumentMismatch,
						sourceEdge.Arg.GetLocation(),
						sourceEdge.Arg.Identifier.Text,
						edge.Param,
						sourceEdge.Param
					) );
				}
			}
		}

		private static ImmutableArray<Edge> GenerateBestMatching( ImmutableArray<Edge> sourceMatching ) {
			ImmutableDictionary<int, int> sourceCost = sourceMatching
				.ToImmutableDictionary( x => x.ArgId, x => x.Cost );

			// split the args and parameters
			var args = sourceMatching.Select( x => Tuple.Create( x.Arg, x.ArgId ) ).ToImmutableArray();
			var @params = sourceMatching.Select( x => x.Param ).ToImmutableArray();

			// get all the matchings. this is super naive and could be selected better
			ImmutableArray<ImmutableArray<Edge>> allMatchings = Permutate( args, @params );

			// never increase the cost of a given edge
			ImmutableArray<ImmutableArray<Edge>> candidateMatchings = allMatchings
				.Where( x => x.All( e => e.Cost <= sourceCost[ e.ArgId ] ) )
				.ToImmutableArray();

			// Select the matching with the minimum cost
			int minCost = int.MaxValue;
			ImmutableArray<Edge> bestMatching;
			foreach( var matching in candidateMatchings ) {
				var cost = matching.Sum( e => e.Cost );
				if( cost < minCost ) {
					minCost = cost;
					bestMatching = matching;
				}
			}

			return bestMatching;
		}

		private static ImmutableArray<ImmutableArray<Edge>> Permutate(
			ImmutableArray<Tuple<IdentifierNameSyntax, int>> args,
			ImmutableArray<string> @params
		) {
			var result = ImmutableArray.CreateBuilder<ImmutableArray<Edge>>();
			Permutate( args, @params, ImmutableArray<Edge>.Empty, result );
			return result.ToImmutableArray();
		}

		private static void Permutate(
			ImmutableArray<Tuple<IdentifierNameSyntax, int>> args,
			ImmutableArray<string> @params,
			ImmutableArray<Edge> current,
			ImmutableArray<ImmutableArray<Edge>>.Builder result
		) {
			if( args.Length == 0 ) {
				result.Add( current );
				return;
			}

			for( int i = 0; i < args.Length; ++i ) {
				for( int j = 0; j < @params.Length; ++j ) {
					ImmutableArray<Edge> perm = current.Add( new Edge(
						args[ i ].Item1,
						args[ i ].Item2,
						@params[ j ]
					) );

					Permutate(
						args.RemoveAt( i ),
						@params.RemoveAt( j ),
						perm,
						result
					);
				}
			}
		}

		private class Edge {
			internal Edge( IdentifierNameSyntax arg, int argId, string param ) {
				Arg = arg;
				ArgId = argId;
				Param = param;
				Cost = LevenshteinDistance( arg.Identifier.Text, param );
			}

			internal IdentifierNameSyntax Arg { get; }
			internal int ArgId { get; }
			internal string Param { get; }
			internal int Cost { get; }
		}

		private static IEnumerable<Edge> MatchArguments(
			SemanticModel model,
			SeparatedSyntaxList<ArgumentSyntax> arguments
		) {
			int i = 0;
			foreach( var arg in arguments ) {
				// Matched explicitly to a parameter
				// Trust the developer
				if( arg.NameColon != null ) {
					yield break;
				}

				// Not a variable, so no name to compare
				var identifer = arg.Expression as IdentifierNameSyntax;
				if( identifer == null ) {
					continue;
				}

				IParameterSymbol param = arg.DetermineParameter(
					model,
					allowParams: false
				);

				// Couldn't get param for some reason, whatever
				if( param == null ) {
					continue;
				}

				// .Name documented as possibly empty, nothing to compare
				if( param.Name == "" ) {
					continue;
				}

				yield return new Edge(
					arg: identifer,
					argId: i++,
					param: param.Name
				);
			}
		}

		// Adapted from https://en.wikipedia.org/wiki/Levenshtein_distance
		private static int LevenshteinDistance( string x, string y ) {
			int m = x.Length;
			int n = y.Length;

			if( m == 0 ) {
				return n;
			}

			if( n == 0 ) {
				return m;
			}

			int[] v0 = new int[ n + 1 ];
			int[] v1 = new int[ n + 1 ];

			for( int i = 0; i < n; ++i ) {
				v0[ i ] = i;
			}

			for( int i = 0; i < m; ++i ) {
				v1[ 0 ] = i + 1;
				for( int j = 0; j < n; ++j ) {
					int deletionCost = v0[ j + 1 ] + 1;
					int insertionCost = v1[ j ] + 1;
					int substituionCost = v0[ j ] +
						x[ i ] == y[ j ] ? 0 : 1;
					v1[ j + 1 ] = Math.Min( deletionCost, Math.Min( insertionCost, substituionCost ) );
				}

				int[] tmp = v0;
				v0 = v1;
				v1 = tmp;
			}
			
			return v0[ n ];
		}
	}
}
