using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.SourceAttribute {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	class ValueSourceAttributeAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0008";
		private const string Category = "Safety";

		private const string Title = "Ensure the source of 'ValueSourceAttribute' is static field, property or method in tests.";
		private const string Description = "The source of 'ValueSourceAttribute' must be static field, property or method in tests.";
		internal const string MessageFormat = "Add 'static' for the source {0} of 'ValueSourceAttribute' for NUnit 3 compatibility";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

		public override void Initialize( AnalysisContext context ) {
			context.RegisterSemanticModelAction(
				AnalyzeSemanticModel
			);
		}

		private void AnalyzeSemanticModel( SemanticModelAnalysisContext semanticModelAnalysisContext ) {
			var tree = semanticModelAnalysisContext.SemanticModel.SyntaxTree;
			var fields = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().ToImmutableArray();
			var properties = tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().ToImmutableArray();
			var methods = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();

			// Get All SourceNames of ValueSourceAttributes
			var memberNames = new List<String>();
			foreach( var method in methods ) {
				var parameters = method.ParameterList.Parameters.ToImmutableArray();
				if( parameters.Length == 0 ) {
					continue;
				}

				foreach( var parameter in parameters ) {
					var attributeLists = parameter.AttributeLists.ToImmutableArray();
					foreach( var attributeList in attributeLists ) {
						var attributes = attributeList.Attributes.ToImmutableArray();
						foreach( var attribute in attributes ) {
							if( attribute.Name.ToString() == "ValueSource" ) {
								var attributeArguments = attribute.ArgumentList.Arguments.ToImmutableArray();
								foreach( var attributeArgument in attributeArguments ) {
									var expression = attributeArgument.Expression;
									//StringLiteralExpression: "a", invocationExpression: nameof(a)  
									if( expression is LiteralExpressionSyntax ) {
										var member = expression.ToString().Replace( "\"", "" ).Replace( "\'", "" ).Trim();
										memberNames.Add( member );

									} else if( expression is InvocationExpressionSyntax ) {
										var member = expression.ToString().Trim().Substring( 7, expression.ToString().Trim().Length - 8 ).Trim();
										memberNames.Add( member );
									}
								}
							}
						}
					}
				}
			}

			// For each Source Report Diagnostic
			foreach( var method in methods ) {
				if( memberNames.Contains( method.Identifier.ToString() ) ) {
					if( !method.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
						var diagnostic = Diagnostic.Create( Rule, method.GetLocation(), "method (and the called methods by it)" );
						semanticModelAnalysisContext.ReportDiagnostic( diagnostic );
					}
				}
			}
			// Fields
			foreach( var field in fields ) {
				var variables = field.Declaration.Variables;
				foreach( var variable in variables ) {
					if( memberNames.Contains( variable.Identifier.ToString() ) ) {
						if( !field.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
							var diagnostic = Diagnostic.Create( Rule, field.GetLocation(), "field" );
							semanticModelAnalysisContext.ReportDiagnostic( diagnostic );
						}
					}
				}
			}
			// Properties
			foreach( var property in properties ) {
				if( memberNames.Contains( property.Identifier.ToString() ) ) {
					if( !property.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
						var diagnostic = Diagnostic.Create( Rule, property.GetLocation(), "property" );
						semanticModelAnalysisContext.ReportDiagnostic( diagnostic );
					}
				}
			}

		}
	}
}
