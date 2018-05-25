using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal static class AssertIsBoolBinaryExpressions {

		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_equalsEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetEqualityOperatorsDiagnostic( 
						e, 
						comparisonReplacementMethodName: nameof( Assert.AreEqual ), 
						nullReplacementMethodName: nameof( Assert.IsNull ), 
						zeroReplacementMethodName: nameof( Assert.Zero ), 
						trueReplacementMethodName: nameof( Assert.IsTrue ), 
						falseReplacementMethodName: nameof( Assert.IsFalse ) 
					),
				() => GetEqualityOperatorsDiagnostic( 
						e, 
						comparisonReplacementMethodName: nameof( Assert.AreNotEqual ), 
						nullReplacementMethodName: nameof( Assert.IsNotNull ), 
						zeroReplacementMethodName: nameof( Assert.NotZero ), 
						trueReplacementMethodName: nameof( Assert.IsFalse ), 
						falseReplacementMethodName: nameof( Assert.IsTrue ) 
					)
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, nameof( Assert.Less ) ),
				() => GetComparisonDiagnostic( e, nameof( Assert.GreaterOrEqual ) )
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, nameof( Assert.LessOrEqual ) ),
				() => GetComparisonDiagnostic( e, nameof( Assert.Greater ) )
			);
		private static readonly Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider> m_isKeywordDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetIsKeywordDiagnostic( e, nameof( Assert.IsInstanceOf ) ),
				() => GetIsKeywordDiagnostic( e, nameof( Assert.IsNotInstanceOf ) )
			);

		private static readonly ImmutableDictionary<SyntaxKind, Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider>> m_diagnosticProviders =
			new Dictionary<SyntaxKind, Func<InvocationExpressionSyntax, AssertIsBoolDiagnosticProvider>> {
				{ SyntaxKind.EqualsEqualsToken, m_equalsEqualsDiagnosticProviderFactory },
				{ SyntaxKind.ExclamationEqualsToken, e => m_equalsEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanToken, m_lessThanDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanToken, e => m_lessThanEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanEqualsToken, m_lessThanEqualsDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanEqualsToken, e => m_lessThanDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.IsKeyword, m_isKeywordDiagnosticProviderFactory }
			}.ToImmutableDictionary();

		public static bool TryGetDiagnosticProvider(
			InvocationExpressionSyntax invocation,
			out AssertIsBoolDiagnosticProvider diagnosticProvider
		) {
			if( invocation.ArgumentList.Arguments.Count == 0 ) {
				diagnosticProvider = null;
				return false;
			}

			if( !( invocation.ArgumentList.Arguments[ 0 ].Expression is BinaryExpressionSyntax binaryExpression ) ) {
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

			return GetDiagnostic( 
					invocation,
					SyntaxFactory.IdentifierName( replacementMethodName ),
					binaryExpression.Left,
					binaryExpression.Right
				);
		}

		private static AssertIsBoolDiagnostic GetEqualityOperatorsDiagnostic(
			InvocationExpressionSyntax invocation,
			string comparisonReplacementMethodName,
			string nullReplacementMethodName,
			string zeroReplacementMethodName,
			string trueReplacementMethodName,
			string falseReplacementMethodName
		) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax) invocation.ArgumentList.Arguments[ 0 ].Expression;

			string replacementMethodName;
			ExpressionSyntax[] firstParameterReplacements;

			bool IsNullLiteral( ExpressionSyntax expression ) =>
				expression.Kind() == SyntaxKind.NullLiteralExpression;

			bool IsZeroLiteral( ExpressionSyntax expression ) =>
				expression.Kind() == SyntaxKind.NumericLiteralExpression && expression.ToString() == "0";

			bool IsTrueLiteral( ExpressionSyntax expression ) =>
				expression.Kind() == SyntaxKind.TrueLiteralExpression;

			bool IsFalseLiteral( ExpressionSyntax expression ) =>
				expression.Kind() == SyntaxKind.FalseLiteralExpression;

			ExpressionSyntax otherOperand;
			if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsNullLiteral, out otherOperand ) ) {
				replacementMethodName = nullReplacementMethodName;
				firstParameterReplacements = new[] { otherOperand };
			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsZeroLiteral, out otherOperand ) ) {
				replacementMethodName = zeroReplacementMethodName;
				firstParameterReplacements = new[] { otherOperand };
			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsTrueLiteral, out otherOperand ) ) {
				replacementMethodName = trueReplacementMethodName;
				firstParameterReplacements = new[] { otherOperand };
			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsFalseLiteral, out otherOperand ) ) {
				replacementMethodName = falseReplacementMethodName;
				firstParameterReplacements = new[] { otherOperand };
			} else {
				replacementMethodName = comparisonReplacementMethodName;
				// switch left and right expressions order because:
				// 1. from what I've seen so far, the first equality operand is the one under test, most of the times
				// 2. most Assert methods usually take the expected value first and the actual value second
				firstParameterReplacements = new[] { binaryExpression.Right, binaryExpression.Left };
			}

			return GetDiagnostic(
					invocation,
					SyntaxFactory.IdentifierName( replacementMethodName ),
					firstParameterReplacements
				);
		}

		private static bool TryGetSingleOperandForEqualityReplacement( 
			this BinaryExpressionSyntax binaryExpression, 
			Predicate<ExpressionSyntax> operandMatch, 
			out ExpressionSyntax otherOperand 
		) {
			if( operandMatch( binaryExpression.Left ) ) {
				otherOperand = binaryExpression.Right;
			} else if( operandMatch( binaryExpression.Right ) ) {
				otherOperand = binaryExpression.Left;
			} else {
				otherOperand = null;
			}

			return otherOperand != null;
		}

		private static AssertIsBoolDiagnostic GetIsKeywordDiagnostic(
			InvocationExpressionSyntax invocation,
			string replacementMethodName
		) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax) invocation.ArgumentList.Arguments[ 0 ].Expression;

			TypeSyntax genericType = SyntaxFactory.ParseTypeName( binaryExpression.Right.ToString() );
			SimpleNameSyntax replacementMethodNameSyntax = SyntaxFactory.GenericName( 
					SyntaxFactory.Identifier( replacementMethodName ),
					SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SingletonSeparatedList( genericType )
						) );

			return GetDiagnostic( invocation, replacementMethodNameSyntax, binaryExpression.Left );
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

			if( firstArgReplacements.Length > 0 ) {
				// first arg has correct leading trivia, or the arg list open parentheses has it;
				// either way, it does not need any additional trivia
				newArgs.Add( SyntaxFactory.Argument( firstArgReplacements[ 0 ].WithoutTrailingTrivia() ) );

				// subsequent new args need the leading trivia for proper alignment
				newArgs.AddRange( firstArgReplacements.Skip( 1 ).Select( 
						r => SyntaxFactory.Argument( r ).WithLeadingTrivia( argLeadingTrivia ).WithoutTrailingTrivia()
					) );

				if( invocation.ArgumentList.Arguments.Count == 1 ) {
					// add back the first argument expression's trivia before the closing parantheses
					SyntaxTriviaList trailingTrivia = invocation.ArgumentList.Arguments[ 0 ].Expression.GetTrailingTrivia();
					int lastIdx = newArgs.Count - 1;
					newArgs[ lastIdx ] = newArgs[ lastIdx ].WithTrailingTrivia( trailingTrivia );
				}
			}

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
			params ExpressionSyntax[] firstArgReplacements
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
