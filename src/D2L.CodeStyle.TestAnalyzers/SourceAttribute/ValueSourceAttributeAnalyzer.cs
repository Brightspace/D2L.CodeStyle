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
			context.RegisterSyntaxNodeAction(
				AnalyzeSyntaxNode,
				SyntaxKind.MethodDeclaration
				);
		}

		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var method = context.Node as MethodDeclarationSyntax;
			if( method == null ) {
				return;
			}

			var parameters = method.ParameterList.Parameters.ToImmutableArray();
			if( parameters.Length == 0 ) {
				return;
			}

			foreach( var parameter in parameters ) {
				var attributeLists = parameter.AttributeLists.ToImmutableArray();
				foreach( var attributeList in attributeLists ) {
					var attributes = attributeList.Attributes.ToImmutableArray();
					foreach( var attribute in attributes ) {
						if( attribute.Name.ToString() == "ValueSource" ) {
							var attributeArguments = attribute.ArgumentList.Arguments.ToImmutableArray();
							String memberType = null;
							String memberName = null;
							ExpressionSyntax nameExpression;
							ITypeSymbol typeContainingMember = null;

							if( attributeArguments.Length == 2 ) {
								var typeExpression = attributeArguments[0].Expression as TypeOfExpressionSyntax;
								memberType = typeExpression.Type.ToString();
								nameExpression = attributeArguments[1].Expression;
								typeContainingMember = context.SemanticModel.GetTypeInfo( typeExpression.Type ).Type;
							} else {
								var methodSymbol = context.SemanticModel.GetDeclaredSymbol( method );
								memberType = methodSymbol.ContainingType.MetadataName;
								nameExpression = attributeArguments[0].Expression;
								typeContainingMember = methodSymbol.ContainingType;
							}

							if( nameExpression is LiteralExpressionSyntax ) {
								memberName = nameExpression.ToString().Trim( new char[] { '\"', '\'', ' ' } );
							} else if( nameExpression is InvocationExpressionSyntax ) {
								InvocationExpressionSyntax invocationExpression = nameExpression as InvocationExpressionSyntax;
								memberName = invocationExpression.ArgumentList.Arguments.First().Expression.ToString();
							}

							var source = typeContainingMember.GetMembers( memberName ).First();
							if( !source.IsStatic ) {
								var diagnostic = Diagnostic.Create( Rule, attribute.GetLocation(), memberName );
								context.ReportDiagnostic( diagnostic );
							}
						}
					}
				}
			}
		}
	}
}
