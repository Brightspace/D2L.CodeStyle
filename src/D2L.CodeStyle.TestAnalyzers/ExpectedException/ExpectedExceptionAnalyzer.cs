using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.ExpectedException {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ExpectedExceptionAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0004";
		private const string Category = "Safety";

		private const string Title = "Ensure test does not contain ExpectedException.";
		private const string Description = "'ExpectedException' should not be used in tests.";
		internal const string MessageFormat = "'ExpectedException' is not safe for use, no longer available in NUnit 3";

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
				SyntaxKind.AttributeList
			);
		}
		
		private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as AttributeListSyntax;
			if( root == null ) {
				return;
			}

			bool isExpectedException = false;
			Location location = root.GetLocation();

			foreach( var attribute in root.Attributes ) {
				if( attribute.Name.ToString().Equals( "ExpectedException" ) ) {
					location = attribute.GetLocation();
					isExpectedException = true;
					break;
				} else if( attribute.Name.ToString().Equals( "TestCase" ) ) {
					foreach( var argument in attribute.ArgumentList.Arguments ) {
						if( argument.NameEquals != null && argument.NameEquals.Name.ToString().Equals( "ExpectedException" ) ) {
							location = argument.GetLocation();
							isExpectedException = true;
							break;
						}
					}
				}
			}

			if( !isExpectedException ) {
				return;
			}

			var diagnostic = Diagnostic.Create( Rule, location );
			context.ReportDiagnostic( diagnostic );
		}
	}
}
