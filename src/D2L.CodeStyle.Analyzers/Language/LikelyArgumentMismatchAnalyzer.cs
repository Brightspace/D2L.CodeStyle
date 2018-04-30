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

			ImmutableArray<Edge> bestMatching = GenerateBestMatching( context.SemanticModel, sourceMatching );

			// We weren't able to find a better matching
			if( bestMatching == null ) {
				return;
			}

			ImmutableDictionary<ArgumentDetails, Edge> sourceEdges = sourceMatching
				.ToImmutableDictionary( x => x.Arg, x => x );
			foreach( var edge in bestMatching ) {
				Edge sourceEdge = sourceEdges[ edge.Arg ];
				if( edge.Param != sourceEdge.Param ) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.LikelyArgumentMismatch,
						sourceEdge.Arg.Location,
						sourceEdge.Arg.Name,
						edge.Param.Name,
						sourceEdge.Param.Name
					) );
				}
			}
		}

		private static ImmutableArray<Edge> GenerateBestMatching( SemanticModel model, ImmutableArray<Edge> sourceMatching ) {
			ImmutableDictionary<ArgumentDetails, int> sourceCost = sourceMatching
				.ToImmutableDictionary( x => x.Arg, x => x.Cost );

			// split the args and parameters
			var args = sourceMatching.Select( x => x.Arg ).ToImmutableArray();
			var @params = sourceMatching.Select( x => x.Param ).ToImmutableArray();

			// get all the matchings. this is super naive and could be selected better
			ImmutableArray<ImmutableArray<Edge>> allMatchings = Permutate( model, args, @params );

			// never increase the cost of a given edge
			ImmutableArray<ImmutableArray<Edge>> candidateMatchings = allMatchings
				.Where( x => x.All( e => e.Cost <= sourceCost[ e.Arg ] ) )
				.ToImmutableArray();

			// Select the matching with the minimum cost
			int minCost = int.MaxValue;
			ImmutableArray<Edge> bestMatching;
			foreach( var matching in candidateMatchings ) {
				var cost = matching.Any( x => x.Cost == int.MaxValue )
					? int.MaxValue
					: matching.Sum( x => x.Cost );
				if( cost < minCost ) {
					minCost = cost;
					bestMatching = matching;
				}
			}

			return bestMatching;
		}

		private static ImmutableArray<ImmutableArray<Edge>> Permutate(
			SemanticModel model,
			ImmutableArray<ArgumentDetails> args,
			ImmutableArray<ParameterDetails> @params
		) {
			var result = ImmutableArray.CreateBuilder<ImmutableArray<Edge>>();
			Permutate( model, args, @params, ImmutableArray<Edge>.Empty, result );
			return result.ToImmutableArray();
		}

		private static void Permutate(
			SemanticModel model,
			ImmutableArray<ArgumentDetails> args,
			ImmutableArray<ParameterDetails> @params,
			ImmutableArray<Edge> current,
			ImmutableArray<ImmutableArray<Edge>>.Builder result
		) {
			if( args.Length == 0 ) {
				result.Add( current );
				return;
			}

			for( int i = 0; i < args.Length; ++i ) {
				var remainingArgs = args.RemoveAt( i );

				for( int j = 0; j < @params.Length; ++j ) {
					var assignment = new Edge(
						model,
						args[ i ],
						@params[ j ]
					);

					// Don't continue with impossible branches
					if( assignment.Cost == int.MaxValue ) {
						continue;
					}

					Permutate(
						model,
						remainingArgs,
						@params.RemoveAt( j ),
						current.Add( assignment ),
						result
					);
				}
			}
		}

		private class Edge {
			internal Edge( SemanticModel model, ArgumentDetails arg, ParameterDetails param ) {
				Arg = arg;
				Param = param;
				Cost = ComputeCost( model );
			}

			internal ArgumentDetails Arg { get; }
			internal ParameterDetails Param { get; }
			internal int Cost { get; }

			private int ComputeCost( SemanticModel model ) {
				Conversion conversion = model.ClassifyConversion( Arg.Syntax, Param.Type );
				if( !conversion.Exists ) {
					return int.MaxValue;
				}

				return LevenshteinDistance( Arg.Name, Param.Name );
			}
		}

		private class ArgumentDetails {
			internal ArgumentDetails(
				string name,
				ExpressionSyntax syntax,
				Location location
			) {
				Name = name;
				Syntax = syntax;
				Location = location;
			}

			internal string Name { get; }
			internal ExpressionSyntax Syntax { get; }
			internal Location Location { get; }
		}

		private class ParameterDetails {
			internal ParameterDetails( string name, ITypeSymbol type ) {
				Name = name;
				Type = type;
			}

			internal string Name { get; }
			internal ITypeSymbol Type { get; }
		}

		private static IEnumerable<Edge> MatchArguments(
			SemanticModel model,
			SeparatedSyntaxList<ArgumentSyntax> arguments
		) {
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

				var argDetails = new ArgumentDetails(
					name: GetArgName( identifer ),
					syntax: identifer,
					location: identifer.GetLocation()
				);

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

				var paramDetails = new ParameterDetails(
					name: param.Name,
					type: param.Type
				);

				yield return new Edge(
					model: model,
					arg: argDetails,
					param: paramDetails
				);
			}
		}

		private static string GetArgName( IdentifierNameSyntax arg ) {
			return arg.Identifier.Text;
		}

		private static ITypeSymbol GetArgType( SemanticModel model, IdentifierNameSyntax arg ) {
			var symbol = model.GetSymbolInfo( arg ).Symbol;

			switch( symbol ) {
				case IParameterSymbol param:
					return param.Type;
				case ILocalSymbol local:
					return local.Type;
				default:
					throw new Exception();
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
