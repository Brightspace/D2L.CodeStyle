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
				() => GetEqualityOperatorsDiagnostic( e ),
				() => GetEqualityOperatorsDiagnostic( e, isNegative: true )
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
				() => GetIsKeywordDiagnostic( e ),
				() => GetIsKeywordDiagnostic( e, isNegative: true )
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

		private static string GetReplacementMethodName( string methodName, string negativeMethodName, bool negative ) => 
			negative ? negativeMethodName : methodName;

		private static AssertIsBoolDiagnostic GetEqualityOperatorsDiagnostic(
			InvocationExpressionSyntax invocation,
			bool isNegative = false
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
				// Assert.IsTrue/IsFalse( x == null ) -> Assert.IsNull/IsNotNull( x )
				// Assert.IsTrue/IsFalse( x != null ) -> Assert.IsNotNull/IsNull( x )
				replacementMethodName = GetReplacementMethodName( 
						nameof( Assert.IsNull ), 
						nameof( Assert.IsNotNull ), 
						isNegative
					);
				firstParameterReplacements = new[] { otherOperand };

			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsZeroLiteral, out otherOperand ) ) {
				// Assert.IsTrue/IsFalse( x == 0 ) -> Assert.Zero/NotZero( x )
				// Assert.IsTrue/IsFalse( x != 0 ) -> Assert.NotZero/Zero( x )
				replacementMethodName = GetReplacementMethodName( 
						nameof( Assert.Zero ), 
						nameof( Assert.NotZero ), 
						isNegative 
					);
				firstParameterReplacements = new[] { otherOperand };

			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsTrueLiteral, out otherOperand ) ) {
				// Assert.IsTrue/IsFalse( x == true ) -> Assert.IsTrue/IsFalse( x )
				// Assert.IsTrue/IsFalse( x != true ) -> Assert.IsFalse/IsTrue( x )
				replacementMethodName = GetReplacementMethodName( 
						nameof( Assert.IsTrue ), 
						nameof( Assert.IsFalse ), 
						isNegative 
					);
				firstParameterReplacements = new[] { otherOperand };

			} else if( binaryExpression.TryGetSingleOperandForEqualityReplacement( IsFalseLiteral, out otherOperand ) ) {
				// Assert.IsTrue/IsFalse( x == false ) -> Assert.IsFalse/IsTrue( x )
				// Assert.IsTrue/IsFalse( x != false ) -> Assert.IsTrue/IsFalse( x )
				replacementMethodName = GetReplacementMethodName( 
						nameof( Assert.IsFalse ), 
						nameof( Assert.IsTrue ), 
						isNegative 
					);
				firstParameterReplacements = new[] { otherOperand };

			} else {
				// Assert.IsTrue/IsFalse( x == y ) -> Assert.AreEqual/AreNotEqual( y, x )
				// Assert.IsTrue/IsFalse( x != y ) -> Assert.AreNotEqual/AreEqual( y, x )
				replacementMethodName = GetReplacementMethodName( 
						nameof( Assert.AreEqual ), 
						nameof( Assert.AreNotEqual ), 
						isNegative 
					);
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
			bool isNegative = false
		) {
			string replacementMethodName = GetReplacementMethodName(
					nameof( Assert.IsInstanceOf ),
					nameof( Assert.IsNotInstanceOf ),
					isNegative
				);

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

			SyntaxToken firstArgReplacementsSeparator = SyntaxFactory.Token( SyntaxKind.CommaToken )
				.WithTrailingTrivia( SyntaxFactory.TriviaList( SyntaxFactory.Space ) );
			if( oldArgumentList.Arguments.Count > 1 ) {
				// get argument separator incl. trivia
				firstArgReplacementsSeparator = oldArgumentList.Arguments.GetSeparator( 0 );
			}
			// this is to properly handle args split accross multi-lines (the new-feed trivia can be on the open paranthesys)
			firstArgReplacementsSeparator = firstArgReplacementsSeparator.WithTrailingTrivia( oldArgumentList.OpenParenToken.TrailingTrivia );

			// trying to align the new args with the existing ones
			ArgumentSyntax oldFirstArg = oldArgumentList.Arguments[ 0 ];
			SyntaxTriviaList argLeadingTrivia = oldFirstArg.HasLeadingTrivia 
				? oldFirstArg.GetLeadingTrivia() 
				: SyntaxTriviaList.Empty;

			List<ArgumentSyntax> newArgs = new List<ArgumentSyntax>();

			if( firstArgReplacements.Length > 0 ) {
				// transfer the leading trivia from the old first argument
				newArgs.Add( SyntaxFactory.Argument( 
						firstArgReplacements[ 0 ].WithLeadingTrivia( oldFirstArg.GetLeadingTrivia() ).WithoutTrailingTrivia() ) 
					);

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
			newSeparators.AddRange( firstArgReplacements.Skip( 1 ).Select( r => firstArgReplacementsSeparator ) );
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
