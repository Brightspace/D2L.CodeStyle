using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ConfigTestSetupStringsAnalyzer : DiagnosticAnalyzer {

		private const string AttributeName = "ConfigTestSetupAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ConfigTestSetupStrings );

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( Register );
		}

		private void Register( CompilationStartAnalysisContext compilation ) {

			compilation.RegisterSyntaxNodeAction(
					AnalyzeSyntaxNode,
					SyntaxKind.MethodDeclaration,
					SyntaxKind.ClassDeclaration
				);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {

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
			if( !attributes.Any() ) {
				return;
			}

			foreach( AttributeSyntax attribute in attributes ) {

				ISymbol symbol = context
					.SemanticModel
					.GetSymbolInfo( attribute )
					.Symbol;

				if( symbol == null ) {
					continue;
				}

				// Not a [ConfigTestSetup()]
				string displayString = symbol.ToDisplayString(
						SymbolDisplayFormat.FullyQualifiedFormat
					);
				if( displayString != AttributeName ) {
					continue;
				}

				SeparatedSyntaxList<AttributeArgumentSyntax> arguments =
					attribute.ArgumentList.Arguments;

				if( arguments.Count != 1 ) {
					continue;
				}

				ExpressionSyntax argExpression = arguments.First().Expression;

				// Not [ConfigTestSetup( "foo" )]
				if( !argExpression.IsKind( SyntaxKind.StringLiteralExpression ) ) {
					continue;
				}

				LiteralExpressionSyntax stringLiteral =
					(LiteralExpressionSyntax)argExpression;

				Diagnostic diagnostic = Diagnostic.Create(
						Diagnostics.ConfigTestSetupStrings,
						argExpression.GetLocation(),
						stringLiteral.ToString().Trim( '"' )
					);
				context.ReportDiagnostic( diagnostic );
			}
		}

	}
}
