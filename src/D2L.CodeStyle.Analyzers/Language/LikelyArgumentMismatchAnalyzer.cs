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
			// context.EnableConcurrentExecution();
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

			ImmutableArray<Edge> matchedArgs = MatchArguments(
				context.SemanticModel,
				expr.ArgumentList.Arguments
			).ToImmutableArray();

			if( matchedArgs.Length <= 1 ) {
				return;
			}

			var args = matchedArgs.Select( x => Tuple.Create( x.Arg, x.ArgNum ) ).ToImmutableArray();
			var @params = matchedArgs.Select( x => x.Param ).ToImmutableArray();

			var baseCostByArg = matchedArgs.ToImmutableDictionary( x => x.ArgNum, x => x.Cost );
			var permutations = Permutations( args.ToArray(), @params.ToArray() );

			var permutationsThatDontIncreaseAnyCost = permutations
				.Where( x => x.All( e => e.Cost <= baseCostByArg[ e.ArgNum ] ) );
			
			var minCost = int.MaxValue;
			ImmutableArray<Edge> bestPermutation;
			foreach( var perm in permutationsThatDontIncreaseAnyCost ) {
				var cost = perm.Sum( e => e.Cost );
				if( cost < minCost ) {
					minCost = cost;
					bestPermutation = perm;
				}
			}

			foreach( var edge in bestPermutation ) {
				var baseEdge = matchedArgs.Single( b => b.ArgNum == edge.ArgNum );
				if( edge.Param != baseEdge.Param ) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.LikelyArgumentMismatch,
						baseEdge.Arg.GetLocation(),
						baseEdge.Arg.Identifier.Text,
						edge.Param,
						baseEdge.Param
					) );
				}
			}
		}

		private static ImmutableArray<ImmutableArray<Edge>> Permutations( Tuple<IdentifierNameSyntax, int>[] args, string[] @params ) {
			List<Edge[]> perms = new List<Edge[]>();
			Permutations( args, @params, new Edge[0], perms );
			return perms.Select( x => x.ToImmutableArray() ).ToImmutableArray();
		}

		private static T[] Without<T>( T[] arr, int i ) {
			List<T> result = new List<T>();
			for( int j = 0; j < arr.Length; ++j ) {
				if( i != j ) {
					result.Add( arr[ j ] );
				}
			}
			return result.ToArray();
		}

		private static void Permutations( Tuple<IdentifierNameSyntax, int>[] args, string[] @params, Edge[] start, List<Edge[]> perms ) {
			if( args.Length == 0 ) {
				perms.Add( start );
				return;
			}

			for( int i = 0; i < args.Length; ++i ) {
				for( int j = 0; j < @params.Length; ++j ) {
					Edge[] perm = new Edge[ start.Length + 1 ];
					start.CopyTo( perm, 0 );
					perm[ perm.Length - 1 ] = new Edge( args[ i ].Item1, args[ i ].Item2, @params[ j ] );

					Permutations( Without( args, i ), Without( @params, j ), perm, perms );
				}
			}
		}

		private class Edge {
			internal Edge( IdentifierNameSyntax arg, int argNum, string param ) {
				Arg = arg;
				ArgNum = argNum;
				Param = param;
				Cost = LevenshteinDistance( arg.Identifier.Text, param );
			}

			internal IdentifierNameSyntax Arg { get; }
			internal int ArgNum { get; }
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

				var identifer = arg.Expression as IdentifierNameSyntax;
				if( identifer == null ) {
					continue;
				}

				IParameterSymbol param = arg.DetermineParameter(
					model,
					allowParams: false
				);

				if( param == null ) {
					continue;
				}

				if( param.Name == "" ) {
					continue;
				}

				yield return new Edge(
					arg: identifer,
					argNum: i++,
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

			// Return the computed edit distance
			return v0[ n ];
		}
	}
}
