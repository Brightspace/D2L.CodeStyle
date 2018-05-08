using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.TestAnalyzers.NUnit.AssertIsBool {

	internal static class AssertIsBoolBinaryExpressions {

		private static readonly AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> m_equalsEqualsDiagnosticProvider =
			new AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>(
				e => GetEqualityOperatorsDiagnostic( e, "AreEqual", "IsNull" ),
				e => GetEqualityOperatorsDiagnostic( e, "AreNotEqual", "IsNotNull" )
			);
		private static readonly AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> m_lessThanDiagnosticProvider =
			new AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>(
				e => GetComparisonDiagnostic( e, "Less" ),
				e => GetComparisonDiagnostic( e, "Greater" )
			);
		private static readonly AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> m_lessThanEqualsDiagnosticProvider =
			new AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>(
				e => GetComparisonDiagnostic( e, "LessOrEqual" ),
				e => GetComparisonDiagnostic( e, "GreaterOrEqual" )
			);
		private static readonly AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> m_isKeywordDiagnosticProvider =
			new AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>(
				e => GetIsKeywordDiagnostic( e, "IsInstanceOf" ),
				e => GetIsKeywordDiagnostic( e, "IsNotInstanceOf" )
			);

		private static readonly ImmutableDictionary<SyntaxKind, AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>> m_diagnosticProviders =
			new Dictionary<SyntaxKind, AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax>> {
				{ SyntaxKind.EqualsEqualsToken, m_equalsEqualsDiagnosticProvider },
				{ SyntaxKind.ExclamationEqualsToken, m_equalsEqualsDiagnosticProvider.Opposite() },

				{ SyntaxKind.LessThanToken, m_lessThanDiagnosticProvider },
				{ SyntaxKind.GreaterThanToken, m_lessThanDiagnosticProvider.Opposite() },

				{ SyntaxKind.LessThanEqualsToken, m_lessThanEqualsDiagnosticProvider },
				{ SyntaxKind.GreaterThanEqualsToken, m_lessThanEqualsDiagnosticProvider.Opposite() },

				{ SyntaxKind.IsKeyword, m_isKeywordDiagnosticProvider }
			}.ToImmutableDictionary();

		public static bool TryGetDiagnosticProvider(
			BinaryExpressionSyntax binaryExpression,
			out AssertIsBoolDiagnosticProvider<BinaryExpressionSyntax> diagnosticProvider
		) {
			return m_diagnosticProviders.TryGetValue( 
					binaryExpression.OperatorToken.Kind(), 
					out diagnosticProvider
				);
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
