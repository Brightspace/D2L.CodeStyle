using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ConfigTestSetupStringsAnalyzer : DiagnosticAnalyzer {

		private const string AttributeTypeName =
			"D2L.LP.Configuration.Config.ConfigTestSetupAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ConfigTestSetupStrings );

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( Register );
		}

		private static void Register( CompilationStartAnalysisContext context ) {

			INamedTypeSymbol attributeType =
				context.Compilation.GetTypeByMetadataName( AttributeTypeName );

			if( attributeType.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					ctx => AnalyzeSyntaxNode( ctx, attributeType ),
					SyntaxKind.MethodDeclaration,
					SyntaxKind.ClassDeclaration
				);
		}

		private static void AnalyzeSyntaxNode(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol attributeType
			) {

			SyntaxList<AttributeListSyntax> attributeList;

			switch( context.Node ) {

				case MethodDeclarationSyntax method:
					attributeList = method.AttributeLists;
					break;

				case ClassDeclarationSyntax @class:
					attributeList = @class.AttributeLists;
					break;

				default:
					return;
			}

			AttributeSyntax[] attributes = attributeList
				.SelectMany( x => x.Attributes )
				.ToArray();

			// Method has no attributes
			if( attributes.Length <= 0 ) {
				return;
			}

			foreach( AttributeSyntax attribute in attributes ) {

				ISymbol symbol = context
					.SemanticModel
					.GetSymbolInfo( attribute )
					.Symbol;

				if( symbol.IsNullOrErrorType() ) {
					continue;
				}

				// Not a [ConfigTestSetup()]
				if( !attributeType.Equals( symbol.ContainingType ) ) {
					continue;
				}

				SeparatedSyntaxList<AttributeArgumentSyntax> arguments =
					attribute.ArgumentList.Arguments;

				if( arguments.Count != 1 ) {
					continue;
				}

				ExpressionSyntax argExpression = arguments.First().Expression;

				// [ConfigTestSetup( nameof(Foo) )]
				if( IsNameOfSyntax( argExpression ) ) {
					continue;
				}

				// Try to provide the most relevant message that we can.
				string messageArg =
					argExpression is LiteralExpressionSyntax literal
						? literal.ToString().Trim( '"' )
						: "...";

				Diagnostic diagnostic = Diagnostic.Create(
						Diagnostics.ConfigTestSetupStrings,
						argExpression.GetLocation(),
						messageArg
					);
				context.ReportDiagnostic( diagnostic );
			}
		}

		private static bool IsNameOfSyntax( ExpressionSyntax expression ) {

			if( !expression.IsKind( SyntaxKind.InvocationExpression ) ) {
				return false;
			}

			InvocationExpressionSyntax invocation =
				(InvocationExpressionSyntax)expression;

			if( !invocation.Expression.IsKind( SyntaxKind.IdentifierName ) ) {
				return false;
			}

			IdentifierNameSyntax identifier =
				(IdentifierNameSyntax)invocation.Expression;

			if( !identifier.Identifier.IsKind( SyntaxKind.IdentifierToken ) ) {
				return false;
			}

			if( identifier.Identifier.Text != "nameof" ) {
				return false;
			}

			return true;
		}

	}
}
