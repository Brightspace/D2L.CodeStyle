using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {

		/// <summary>
		/// Given a base type or interface, locates the TypeDeclarationSyntax which implements the type
		/// along with the relevant BaseTypeSyntax. If the declaration implementing the type can't be found,
		/// returns any Declaration and a null BaseType. Both are null if there is no declaring syntax.
		/// </summary>
		/// <exception cref="ArgumentException">If <paramref name="baseTypeOrInterface"/> is not implemented by <paramref name="this"/></exception>
		public static (TypeDeclarationSyntax? Delcaration, BaseTypeSyntax? BaseType) ExpensiveGetSyntaxImplementingType(
			this INamedTypeSymbol @this,
			INamedTypeSymbol baseTypeOrInterface,
			Compilation compilation,
			CancellationToken cancellationToken
		) {
			if( !(
				baseTypeOrInterface.Equals( @this.BaseType, SymbolEqualityComparer.Default )
				|| @this.Interfaces.Contains( baseTypeOrInterface, SymbolEqualityComparer.Default )
			) ) {
				throw new ArgumentException( "not implemented by this type", nameof( baseTypeOrInterface ) );
			}

			TypeDeclarationSyntax? anyDeclaration = null;
			foreach( var reference in @this.DeclaringSyntaxReferences ) {
				var declaration = (TypeDeclarationSyntax)reference.GetSyntax( cancellationToken );
				anyDeclaration = declaration;

				var baseTypes = declaration.BaseList?.Types;
				if( baseTypes is null ) {
					continue;
				}

				SemanticModel model = compilation.GetSemanticModel( declaration.SyntaxTree );
				foreach( BaseTypeSyntax baseType in baseTypes ) {
					ITypeSymbol? thisTypeSymbol = model.GetTypeInfo( baseType.Type, cancellationToken ).Type;

					if( baseTypeOrInterface.Equals( thisTypeSymbol, SymbolEqualityComparer.Default ) ) {
						return (declaration, baseType);
					}
				}
			}

			return (anyDeclaration, null);
		}

		// Adapted from /src/Workspaces/CSharp/Portable/Extensions/ArgumentSyntaxExtensions.cs in Roslyn
		// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.

		/// <summary>
		/// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
		/// is true, the last parameter will be returned if it is params parameter and the index of
		/// the specified argument is greater than the number of parameters.
		/// </summary>
		public static IParameterSymbol? DetermineParameter(
			this ArgumentSyntax argument,
			SemanticModel semanticModel,
			bool allowParams,
			CancellationToken cancellationToken
		) {
			var argumentList = argument.Parent as BaseArgumentListSyntax;
			if( argumentList == null ) {
				return null;
			}

			SyntaxNode invocableExpression;
			switch( argumentList.Parent ) {
				case ConstructorInitializerSyntax altConstructorCall:
					// : this( func ) or : base( func )
					invocableExpression = altConstructorCall;
					break;
				case ExpressionSyntax regularCall:
					// Bar( func )
					invocableExpression = regularCall;
					break;
				default:
					return null;
			}

			var symbol = semanticModel.GetSymbolInfo( invocableExpression, cancellationToken ).Symbol;
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
