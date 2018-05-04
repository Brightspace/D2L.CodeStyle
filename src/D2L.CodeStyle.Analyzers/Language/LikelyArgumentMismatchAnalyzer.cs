﻿using System;
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

			ImmutableArray<Edge> bestMatching = GenerateBestMatching( context, expr );

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

		private static ImmutableArray<Edge> GenerateBestMatching(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax expr
		) {
			var parameters = GetPossibleParameters( context, expr );

			int[,] costMatrix = GetCostMatrix(
				expr.ArgumentList.Arguments,
				parameters,
				context.SemanticModel
			);

			int[] assignments = HungarianAlgorithm.FindAssignments( costMatrix );
			ImmutableArray<Edge> matching = ConvertToMatching(
				assignments,
				expr.ArgumentList.Arguments,
				parameters
			);

			return matching;
		}

		private static int[,] GetCostMatrix(
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			ImmutableArray<IParameterSymbol> parameters,
			SemanticModel model
		) {
			int[,] matrix = new int[ arguments.Count, parameters.Count() ];

			int i = 0;
			foreach( ArgumentSyntax arg in arguments ) {

				int j = 0;
				foreach( IParameterSymbol parameter in parameters ) {
					int cost = GetCost( arg, parameter, model );
					matrix[ i, j ] = cost;
					j++;
				}

				i++;
			}

			return matrix;
		}

		private static int GetCost(
			ArgumentSyntax arg,
			IParameterSymbol parameter,
			SemanticModel model
		) {
			if( arg.NameColon != null && !arg.NameColon.IsMissing ) {
				// named parameter, trust the developer
				return 0;
			}

			// Not a variable, so no name to compare, must be correct
			var identifer = arg.Expression as IdentifierNameSyntax;
			if( identifer == null ) {
				return 0;
			}

			ITypeSymbol argType = model.GetTypeInfo( identifer ).Type;
			ITypeSymbol parameterType = parameter.Type;

			int costMultiplier = 1;
			Conversion conversion = model.ClassifyConversion( identifer, parameterType );
			if( !conversion.Exists ) {
				// if the types don't match, make the cost large so we don't choose it
				costMultiplier = 10;
			}

			// Couldn't get param for some reason, whatever
			if( parameter == null ) {
				return 0;
			}

			// .Name documented as possibly empty, nothing to compare
			if( parameter.Name == "" ) {
				return 0;
			}

			int cost = LevenshteinDistance(
				identifer.Identifier.Text,
				parameter.Name
			) * costMultiplier;

			return cost;
		}

		private static ImmutableArray<Edge> ConvertToMatching(
			int[] assignments,
			SeparatedSyntaxList<ArgumentSyntax> arguments,
			ImmutableArray<IParameterSymbol> parameters
		) {
			List<Edge> edges = new List<Edge>();

			int i = -1;
			foreach( ArgumentSyntax arg in arguments ) {
				i++;
				int paramIndex = assignments[ i ];
				IParameterSymbol param = parameters.ElementAt( paramIndex );

				var identifer = arg.Expression as IdentifierNameSyntax;
				if( identifer == null ) {
					continue;
				}

				// Couldn't get param for some reason, whatever
				if( param == null ) {
					continue;
				}

				// .Name documented as possibly empty, nothing to compare
				if( param.Name == "" ) {
					continue;
				}

				var argDetails = new ArgumentDetails(
					name: GetArgName( identifer ),
					syntax: identifer,
					location: identifer.GetLocation()
				);

				var paramDetails = new ParameterDetails(
					name: param.Name,
					type: param.Type
				);

				edges.Add( new Edge(
					arg: argDetails,
					param: paramDetails
				) );
			}

			return edges.ToImmutableArray();
		}

		private static ImmutableArray<IParameterSymbol> GetPossibleParameters(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax expr
		) {
			ImmutableArray < IParameterSymbol > parameters = ImmutableArray<IParameterSymbol>.Empty;

			var symbol = context.SemanticModel.GetSymbolInfo( expr ).Symbol;
			if( symbol == null ) {
				return parameters;
			}

			// This is MS's GetParameters extension, inlined.
			// It's ugly because we don't have new C# features yet.
			if( symbol is IMethodSymbol ) {
				parameters = ( (IMethodSymbol)symbol ).Parameters;
			} else if( symbol is IPropertySymbol ) {
				parameters = ( (IPropertySymbol)symbol ).Parameters;
			} else {
				parameters = ImmutableArray.Create<IParameterSymbol>();
			}

			return parameters;
		}

		private class Edge {
			internal Edge( SemanticModel model, ArgumentDetails arg, ParameterDetails param ) {
				Arg = arg;
				Param = param;
				Cost = ComputeCost( model );
			}

			internal Edge( ArgumentDetails arg, ParameterDetails param ) {
				Arg = arg;
				Param = param;
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

			public override bool Equals( object obj ) {
				if( obj == null ) {
					return false;
				}

				var other = obj as ArgumentDetails;
				if( other == null ) {
					return false;
				}

				return this.Name == other.Name;
			}

			public override int GetHashCode() {
				unchecked {
					int hash = 13;
					hash = ( hash * 7 ) + this.Name.GetHashCode();
					return hash;
				}
			}
		}

		private class ParameterDetails {
			internal ParameterDetails( string name, ITypeSymbol type ) {
				Name = name;
				Type = type;
			}

			internal string Name { get; }
			internal ITypeSymbol Type { get; }

			public override bool Equals( object obj ) {
				if( obj == null ) {
					return false;
				}

				var other = obj as ParameterDetails;
				if( other == null ) {
					return false;
				}

				return this.Name == other.Name;
			}

			public override int GetHashCode() {
				unchecked {
					int hash = 13;
					hash = ( hash * 7 ) + this.Name.GetHashCode();

					return hash;
				}
			}

			public static bool operator == (ParameterDetails p1, ParameterDetails p2 ) {
				return p1.Name == p2.Name;
			}

			public static bool operator !=( ParameterDetails p1, ParameterDetails p2 ) {
				return p1.Name != p2.Name;
			}
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

			for( int i = 0; i < n + 1; ++i ) {
				v0[ i ] = i;
			}

			for( int i = 0; i < m; ++i ) {
				v1[ 0 ] = i + 1;
				for( int j = 0; j < n; ++j ) {
					int deletionCost = v0[ j + 1 ] + 1;
					int insertionCost = v1[ j ] + 1;
					int substituionCost = v0[ j ] +
						( x[ i ] == y[ j ] ? 0 : 1 );
					v1[ j + 1 ] = Math.Min( deletionCost, Math.Min( insertionCost, substituionCost ) );
				}

				int[] tmp = v0;
				v0 = v1;
				v1 = tmp;
			}

			return v0[ n ];
		}


		#region Hungarian Algorithm
		// https://github.com/vivet/HungarianAlgorithm/blob/master/HungarianAlgorithm/HungarianAlgorithm.cs
		/// <summary>
		/// Hungarian Algorithm.
		/// </summary>
		private static class HungarianAlgorithm {
			/// <summary>
			/// Finds the optimal assignments for a given matrix of agents and costed tasks such that the total cost is minimized.
			/// </summary>
			/// <param name="costs">A cost matrix; the element at row <em>i</em> and column <em>j</em> represents the cost of agent <em>i</em> performing task <em>j</em>.</param>
			/// <returns>A matrix of assignments; the value of element <em>i</em> is the column of the task assigned to agent <em>i</em>.</returns>
			/// <exception cref="ArgumentNullException"><paramref name="costs"/> is null.</exception>
			public static int[] FindAssignments( int[,] costs ) {
				if( costs == null )
					throw new ArgumentNullException( nameof( costs ) );

				var h = costs.GetLength( 0 );
				var w = costs.GetLength( 1 );

				for( var i = 0; i < h; i++ ) {
					var min = int.MaxValue;

					for( var j = 0; j < w; j++ ) {
						min = Math.Min( min, costs[ i, j ] );
					}

					for( var j = 0; j < w; j++ ) {
						costs[ i, j ] -= min;
					}
				}

				var masks = new byte[ h, w ];
				var rowsCovered = new bool[ h ];
				var colsCovered = new bool[ w ];

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( costs[ i, j ] == 0 && !rowsCovered[ i ] && !colsCovered[ j ] ) {
							masks[ i, j ] = 1;
							rowsCovered[ i ] = true;
							colsCovered[ j ] = true;
						}
					}
				}

				HungarianAlgorithm.ClearCovers( rowsCovered, colsCovered, w, h );

				var path = new Location[ w * h ];
				var pathStart = default( Location );
				var step = 1;

				while( step != -1 ) {
					switch( step ) {
						case 1:
							step = HungarianAlgorithm.RunStep1( masks, colsCovered, w, h );
							break;

						case 2:
							step = HungarianAlgorithm.RunStep2( costs, masks, rowsCovered, colsCovered, w, h, ref pathStart );
							break;

						case 3:
							step = HungarianAlgorithm.RunStep3( masks, rowsCovered, colsCovered, w, h, path, pathStart );
							break;

						case 4:
							step = HungarianAlgorithm.RunStep4( costs, rowsCovered, colsCovered, w, h );
							break;
					}
				}

				var agentsTasks = new int[ h ];

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( masks[ i, j ] == 1 ) {
							agentsTasks[ i ] = j;
							break;
						}
					}
				}

				return agentsTasks;
			}

			private static int RunStep1( byte[,] masks, bool[] colsCovered, int w, int h ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( masks[ i, j ] == 1 )
							colsCovered[ j ] = true;
					}
				}

				var colsCoveredCount = 0;

				for( var j = 0; j < w; j++ ) {
					if( colsCovered[ j ] )
						colsCoveredCount++;
				}

				if( colsCoveredCount == h )
					return -1;

				return 2;
			}
			private static int RunStep2( int[,] costs, byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, ref Location pathStart ) {
				if( costs == null )
					throw new ArgumentNullException( nameof( costs ) );

				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				while( true ) {
					var loc = HungarianAlgorithm.FindZero( costs, rowsCovered, colsCovered, w, h );
					if( loc.row == -1 )
						return 4;

					masks[ loc.row, loc.column ] = 2;

					var starCol = HungarianAlgorithm.FindStarInRow( masks, w, loc.row );
					if( starCol != -1 ) {
						rowsCovered[ loc.row ] = true;
						colsCovered[ starCol ] = false;
					} else {
						pathStart = loc;
						return 3;
					}
				}
			}
			private static int RunStep3( byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, Location[] path, Location pathStart ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				var pathIndex = 0;
				path[ 0 ] = pathStart;

				while( true ) {
					var row = HungarianAlgorithm.FindStarInColumn( masks, h, path[ pathIndex ].column );
					if( row == -1 )
						break;

					pathIndex++;
					path[ pathIndex ] = new Location( row, path[ pathIndex - 1 ].column );

					var col = HungarianAlgorithm.FindPrimeInRow( masks, w, path[ pathIndex ].row );

					pathIndex++;
					path[ pathIndex ] = new Location( path[ pathIndex - 1 ].row, col );
				}

				HungarianAlgorithm.ConvertPath( masks, path, pathIndex + 1 );
				HungarianAlgorithm.ClearCovers( rowsCovered, colsCovered, w, h );
				HungarianAlgorithm.ClearPrimes( masks, w, h );

				return 1;
			}
			private static int RunStep4( int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h ) {
				if( costs == null )
					throw new ArgumentNullException( nameof( costs ) );

				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				var minValue = HungarianAlgorithm.FindMinimum( costs, rowsCovered, colsCovered, w, h );

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( rowsCovered[ i ] )
							costs[ i, j ] += minValue;
						if( !colsCovered[ j ] )
							costs[ i, j ] -= minValue;
					}
				}
				return 2;
			}

			private static int FindMinimum( int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h ) {
				if( costs == null )
					throw new ArgumentNullException( nameof( costs ) );

				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				var minValue = int.MaxValue;

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( !rowsCovered[ i ] && !colsCovered[ j ] )
							minValue = Math.Min( minValue, costs[ i, j ] );
					}
				}

				return minValue;
			}
			private static int FindStarInRow( byte[,] masks, int w, int row ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				for( var j = 0; j < w; j++ ) {
					if( masks[ row, j ] == 1 )
						return j;
				}

				return -1;
			}
			private static int FindStarInColumn( byte[,] masks, int h, int col ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				for( var i = 0; i < h; i++ ) {
					if( masks[ i, col ] == 1 )
						return i;
				}

				return -1;
			}
			private static int FindPrimeInRow( byte[,] masks, int w, int row ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				for( var j = 0; j < w; j++ ) {
					if( masks[ row, j ] == 2 )
						return j;
				}

				return -1;
			}
			private static Location FindZero( int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h ) {
				if( costs == null )
					throw new ArgumentNullException( nameof( costs ) );

				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( costs[ i, j ] == 0 && !rowsCovered[ i ] && !colsCovered[ j ] )
							return new Location( i, j );
					}
				}

				return new Location( -1, -1 );
			}
			private static void ConvertPath( byte[,] masks, Location[] path, int pathLength ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				if( path == null )
					throw new ArgumentNullException( nameof( path ) );

				for( var i = 0; i < pathLength; i++ ) {
					if( masks[ path[ i ].row, path[ i ].column ] == 1 ) {
						masks[ path[ i ].row, path[ i ].column ] = 0;
					} else if( masks[ path[ i ].row, path[ i ].column ] == 2 ) {
						masks[ path[ i ].row, path[ i ].column ] = 1;
					}
				}
			}
			private static void ClearPrimes( byte[,] masks, int w, int h ) {
				if( masks == null )
					throw new ArgumentNullException( nameof( masks ) );

				for( var i = 0; i < h; i++ ) {
					for( var j = 0; j < w; j++ ) {
						if( masks[ i, j ] == 2 )
							masks[ i, j ] = 0;
					}
				}
			}
			private static void ClearCovers( bool[] rowsCovered, bool[] colsCovered, int w, int h ) {
				if( rowsCovered == null )
					throw new ArgumentNullException( nameof( rowsCovered ) );

				if( colsCovered == null )
					throw new ArgumentNullException( nameof( colsCovered ) );

				for( var i = 0; i < h; i++ ) {
					rowsCovered[ i ] = false;
				}

				for( var j = 0; j < w; j++ ) {
					colsCovered[ j ] = false;
				}
			}

			private struct Location {
				public readonly int row;
				public readonly int column;

				public Location( int row, int col ) {
					this.row = row;
					this.column = col;
				}
			}
		}
		#endregion
	}
}
