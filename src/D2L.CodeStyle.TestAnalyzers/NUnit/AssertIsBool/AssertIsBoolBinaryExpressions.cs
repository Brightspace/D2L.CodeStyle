using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal static class AssertIsBoolBinaryExpressions {

		private static readonly Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider> m_equalsEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetEqualityOperatorsDiagnostic( e, "AreEqual", "IsNull" ),
				() => GetEqualityOperatorsDiagnostic( e, "AreNotEqual", "IsNotNull" )
			);
		private static readonly Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, "Less" ),
				() => GetComparisonDiagnostic( e, "Greater" )
			);
		private static readonly Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider> m_lessThanEqualsDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetComparisonDiagnostic( e, "LessOrEqual" ),
				() => GetComparisonDiagnostic( e, "GreaterOrEqual" )
			);
		private static readonly Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider> m_isKeywordDiagnosticProviderFactory =
			e => new AssertIsBoolDiagnosticProvider(
				() => GetIsKeywordDiagnostic( e, "IsInstanceOf" ),
				() => GetIsKeywordDiagnostic( e, "IsNotInstanceOf" )
			);

		private static readonly ImmutableDictionary<SyntaxKind, Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider>> m_diagnosticProviders =
			new Dictionary<SyntaxKind, Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider>> {
				{ SyntaxKind.EqualsEqualsToken, m_equalsEqualsDiagnosticProviderFactory },
				{ SyntaxKind.ExclamationEqualsToken, e => m_equalsEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanToken, m_lessThanDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanToken, e => m_lessThanDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.LessThanEqualsToken, m_lessThanEqualsDiagnosticProviderFactory },
				{ SyntaxKind.GreaterThanEqualsToken, e => m_lessThanEqualsDiagnosticProviderFactory( e ).Opposite() },

				{ SyntaxKind.IsKeyword, m_isKeywordDiagnosticProviderFactory }
			}.ToImmutableDictionary();

		public static bool TryGetDiagnosticProvider(
			BinaryExpressionSyntax binaryExpression,
			out AssertIsBoolDiagnosticProvider diagnosticProvider
		) {
			Func<BinaryExpressionSyntax, AssertIsBoolDiagnosticProvider> diagnosticProviderFactory;
			bool knownExpression = m_diagnosticProviders.TryGetValue( 
					binaryExpression.OperatorToken.Kind(), 
					out diagnosticProviderFactory
				);

			if( !knownExpression ) {
				diagnosticProvider = null;
				return false;
			}

			diagnosticProvider = diagnosticProviderFactory( binaryExpression );
			return true;
		}

		private static string GetComparisonDiagnostic(
			BinaryExpressionSyntax binaryExpression,
			string replacementMethodName
		) {
			string otherArgsStr = GetOtherArgumentsAsString( binaryExpression );

			string diagnostic = $"Assert.{replacementMethodName}( {binaryExpression.Left}, {binaryExpression.Right}{otherArgsStr} )";
			return diagnostic;
		}

		private static string GetEqualityOperatorsDiagnostic(
			BinaryExpressionSyntax binaryExpression,
			string nonNullReplacementMethodName,
			string nullReplacementMethodName
		) {
			ExpressionSyntax nonNullOperand = null;
			if( binaryExpression.Left.Kind() == SyntaxKind.NullLiteralExpression ) {
				nonNullOperand = binaryExpression.Right;
			} else if( binaryExpression.Right.Kind() == SyntaxKind.NullLiteralExpression ) {
				nonNullOperand = binaryExpression.Left;
			}

			if( nonNullOperand == null ) {
				return GetComparisonDiagnostic( binaryExpression, nonNullReplacementMethodName );
			}

			string otherArgsStr = GetOtherArgumentsAsString( binaryExpression );

			string diagnostic = $"Assert.{nullReplacementMethodName}( {nonNullOperand}{otherArgsStr} )";
			return diagnostic;
		}

		private static string GetIsKeywordDiagnostic(
			BinaryExpressionSyntax binaryExpression,
			string replacementMethodName
		) {
			string otherArgsStr = GetOtherArgumentsAsString( binaryExpression );

			string diagnostic = $"Assert.{replacementMethodName}<{binaryExpression.Right}>( {binaryExpression.Left}{otherArgsStr} )";
			return diagnostic;
		}

		private static string GetOtherArgumentsAsString( BinaryExpressionSyntax binaryExpression ) {
			ArgumentListSyntax fullArgumentsList = ( ArgumentListSyntax ) binaryExpression.Parent.Parent;
			SeparatedSyntaxList<ArgumentSyntax> otherArgs = fullArgumentsList.Arguments.RemoveAt( 0 );

			var otherArgsStr = "";
			if( otherArgs.Count > 0 ) {
				string separator = fullArgumentsList.Arguments.GetSeparator( 0 ).ValueText;
				otherArgsStr = $"{separator} {otherArgs.GetWithSeparators()}";
			}

			return otherArgsStr;
		}
	}
}
