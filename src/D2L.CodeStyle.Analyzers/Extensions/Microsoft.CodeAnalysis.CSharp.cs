using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {

		// Adapted from /src/Workspaces/CSharp/Portable/Extensions/ArgumentSyntaxExtensions.cs in Roslyn
		// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.

		/// <summary>
		/// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
		/// is true, the last parameter will be returned if it is params parameter and the index of
		/// the specified argument is greater than the number of parameters.
		/// </summary>
		public static IParameterSymbol DetermineParameter(
			this ArgumentSyntax argument,
			SemanticModel semanticModel,
			bool allowParams = false
		) {
			var argumentList = argument.Parent as BaseArgumentListSyntax;
			if( argumentList == null ) {
				return null;
			}

			var invocableExpression = argumentList.Parent as ExpressionSyntax;
			if( invocableExpression == null ) {
				return null;
			}

			var symbol = semanticModel.GetSymbolInfo( invocableExpression ).Symbol;
			if( symbol == null ) {
				return null;
			}

			// This is MS's GetParameters extension, inlined.
			// It's ugly because we don't have new C# features yet.
			ImmutableArray<IParameterSymbol> parameters;
			if ( symbol is IMethodSymbol ) {
				parameters = ((IMethodSymbol)symbol).Parameters;
			} else if ( symbol is IPropertySymbol ) {
				parameters = ( (IPropertySymbol)symbol ).Parameters;
			} else {
				parameters = ImmutableArray.Create<IParameterSymbol>();
			}

			// Handle named argument
			if( argument.NameColon != null && !argument.NameColon.IsMissing ) {
				var name = argument.NameColon.Name.Identifier.ValueText;
				return parameters.FirstOrDefault( p => p.Name == name );
			}

			// Handle positional argument
			var index = argumentList.Arguments.IndexOf( argument );
			if( index < 0 ) {
				return null;
			}

			if( index < parameters.Length ) {
				return parameters[index];
			}

			if( allowParams ) {
				var lastParameter = parameters.LastOrDefault();
				if( lastParameter == null ) {
					return null;
				}

				if( lastParameter.IsParams ) {
					return lastParameter;
				}
			}

			return null;
		}
	}
}
