using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.TestAnalyzers.IgnoreAttribute {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class IgnoreAttributeAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0009";
		private const string Category = "Safety";

		private const string Title = "Ensure test contains reason for IgnoreAttribute.";
		private const string Description = "The reason for IgnoreAttribute is now mandatory.";
		internal const string MessageFormat = "Add reason for IgnoreAttribute for NUnit 3 compatibility";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
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

			foreach( var attribute in root.Attributes ) {
				if( attribute.Name.ToString().Equals( "Ignore" ) ) {
					var parentClassDeclaration = root.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().ToImmutableArray().Single();
					foreach( var parentAttributeList in parentClassDeclaration.AttributeLists ) {
						foreach( var parentAttribute in parentAttributeList.Attributes ) {
							if( parentAttribute.Name.ToString().Equals( "ReflectionSerializer" ) ) {
								return;
							}
						}
					}

					if( attribute.ArgumentList == null ) {
						var diagnostic = Diagnostic.Create( Rule, attribute.GetLocation() );
						context.ReportDiagnostic( diagnostic );
					}
				}
			}
		}
	}
}
