using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal static class AssertIsBoolBinaryExpressions {

		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_equalsEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetEqualityOperatorsDiagnostic( e, "AreEqual", "IsNull" ),
				() => GetEqualityOperatorsDiagnostic( e, "AreNotEqual", "IsNotNull" )
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, "Less" ),
				() => GetComparisonDiagnostic( e, "Greater" )
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, "LessOrEqual" ),
				() => GetComparisonDiagnostic( e, "GreaterOrEqual" )
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_isKeywordDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetIsKeywordDiagnostic( e, "IsInstanceOf" ),
				() => GetIsKeywordDiagnostic( e, "IsNotInstanceOf" )
			);

		private static readonly ImmutableDictionary<SyntaxKind, Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider>> m_diagnosticProviders =
			new Dictionary<SyntaxKind, Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider>> {
				{ SyntaxKind.EqualsEqualsToken, m_equalsEqualsDiagnosticProviderFactory },
				{ SyntaxKind.ExclamationEqualsToken, e => m_equalsEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanToken, m_lessThanDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanToken, e => m_lessThanDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanEqualsToken, m_lessThanEqualsDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanEqualsToken, e => m_lessThanEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.IsKeyword, m_isKeywordDiagnosticProviderFactory }
			}.ToImmutableDictionary();

		public static bool TryGetDiagnosticProvider(
			InvocationExpressionSyntax invocation,
			out AssertIsBoolDiagnosticProvider diagnosticProvider
		) {
			ArgumentSyntax firstArgument = invocation.ArgumentList.Arguments[ 0 ];
			if( !( firstArgument.Expression is BinaryExpressionSyntax binaryExpression ) ) {
				diagnosticProvider = null;
				return false;
			}

			bool isKnownExpression = m_diagnosticProviders.TryGetValue(
				binaryExpression.OperatorToken.Kind(),
				out Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> diagnosticProviderFactory
			);

			if( !isKnownExpression ) {
				diagnosticProvider = null;
				return false;
			}

			diagnosticProvider = diagnosticProviderFactory( invocation );
			return true;
		}

		private static AssertIsBoolDiagnostic GetComparisonDiagnostic(
			InvocationExpressionSyntax invocation,
			string replacementMethodName
		) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax) invocation.ArgumentList.Arguments[ 0 ].Expression;
			ExpressionSyntax[] firstArgReplacements = {
					binaryExpression.Left,
					binaryExpression.Right
				};

			SimpleNameSyntax replacementMethodNameSyntax = SyntaxFactory.IdentifierName( replacementMethodName );

			return GetDiagnostic( invocation, replacementMethodNameSyntax, firstArgReplacements );
		}

		private static AssertIsBoolDiagnostic GetEqualityOperatorsDiagnostic(
			InvocationExpressionSyntax invocation,
			string nonNullReplacementMethodName,
			string nullReplacementMethodName
		) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax) invocation.ArgumentList.Arguments[ 0 ].Expression;

			ExpressionSyntax nonNullOperand = null;
			if( binaryExpression.Left.Kind() == SyntaxKind.NullLiteralExpression ) {
				nonNullOperand = binaryExpression.Right;
			} else if( binaryExpression.Right.Kind() == SyntaxKind.NullLiteralExpression ) {
				nonNullOperand = binaryExpression.Left;
			}

			if( nonNullOperand == null ) {
				return GetComparisonDiagnostic( invocation, nonNullReplacementMethodName );
			}

			ExpressionSyntax[] firstArgReplacements = { nonNullOperand };

			SimpleNameSyntax replacementMethodNameSyntax = SyntaxFactory.IdentifierName( nullReplacementMethodName );

			return GetDiagnostic( invocation, replacementMethodNameSyntax, firstArgReplacements );
		}

		private static AssertIsBoolDiagnostic GetIsKeywordDiagnostic(
			InvocationExpressionSyntax invocation,
			string replacementMethodName
		) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax) invocation.ArgumentList.Arguments[ 0 ].Expression;

			ExpressionSyntax[] firstArgReplacements = { binaryExpression.Left };

			SimpleNameSyntax replacementMethodNameSyntax = SyntaxFactory.GenericName( 
					SyntaxFactory.Identifier( replacementMethodName ),
					SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SingletonSeparatedList( SyntaxFactory.ParseTypeName( binaryExpression.Right.ToString() ) )
						) );

			return GetDiagnostic( invocation, replacementMethodNameSyntax, firstArgReplacements );
		}

		private static ArgumentListSyntax FormatArgumentList(
			InvocationExpressionSyntax invocation,
			ExpressionSyntax[] firstArgReplacements
		) {
			ArgumentListSyntax oldArgumentList = invocation.ArgumentList;

			SyntaxToken separator = SyntaxFactory.Token( SyntaxKind.CommaToken );
			if( oldArgumentList.Arguments.Count > 1 ) {
				// get argument separator incl. trivia
				separator = oldArgumentList.Arguments.GetSeparator( 0 );
			}

			// trying to align the new args with the existing ones
			ArgumentSyntax oldFirstArg = oldArgumentList.Arguments[ 0 ];
			SyntaxTriviaList argLeadingTrivia = oldFirstArg.HasLeadingTrivia 
				? oldFirstArg.GetLeadingTrivia() 
				: SyntaxFactory.TriviaList( SyntaxFactory.Space );

			List<ArgumentSyntax> newArgs = new List<ArgumentSyntax>();

			// first arg has correct leading trivia, or the arg list open parentheses has it;
			// either way, it does not need any additional trivia
			if( firstArgReplacements.Length > 0 ) {
				newArgs.Add( SyntaxFactory.Argument( firstArgReplacements[ 0 ] ) );
			}
			// subsequent new args need the leading trivia for proper alignment
			newArgs.AddRange( firstArgReplacements.Skip( 1 ).Select( 
					r => SyntaxFactory.Argument( r ).WithLeadingTrivia( argLeadingTrivia ) 
				) );
			newArgs.AddRange( invocation.ArgumentList.Arguments.Skip( 1 ) );

			List<SyntaxToken> newSeparators = new List<SyntaxToken>();
			newSeparators.AddRange( firstArgReplacements.Skip( 1 ).Select( r => separator ) );
			newSeparators.AddRange( oldArgumentList.Arguments.GetSeparators() );

			ArgumentListSyntax newArgumentList = SyntaxFactory.ArgumentList(
					oldArgumentList.OpenParenToken,
					SyntaxFactory.SeparatedList( newArgs, newSeparators ),
					oldArgumentList.CloseParenToken 
				);

			return newArgumentList;
		}

		private static AssertIsBoolDiagnostic GetDiagnostic(
			InvocationExpressionSyntax invocation,
			SimpleNameSyntax replacementMethodNameSyntax,
			ExpressionSyntax[] firstArgReplacements
		) {
			ExpressionSyntax classNameSyntax = ( (MemberAccessExpressionSyntax) invocation.Expression ).Expression;

			ArgumentListSyntax newArgumentList = FormatArgumentList(
				invocation,
				firstArgReplacements
			);

			ExpressionSyntax expt = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					classNameSyntax,
					SyntaxFactory.Token( SyntaxKind.DotToken ),
					replacementMethodNameSyntax
				),
				newArgumentList
			);

			string message = $"{classNameSyntax}.{replacementMethodNameSyntax}";

			return new AssertIsBoolDiagnostic( message, expt );
		}
	}
}
