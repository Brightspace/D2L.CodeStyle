using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousAttributeUsages {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class DangerousAttributeUsages : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousAttributesShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeAttribute( ctx ),
				SyntaxKind.Attribute
			);
		}

		private void AnalyzeAttribute( SyntaxNodeAnalysisContext context ) {

			AttributeSyntax attribute = context.Node as AttributeSyntax;
			if( attribute == null ) {
				return;
			}

			if( !DangerousAttributes.Definitions.Contains( attribute.Name.ToString() ) ) {
				return;
			}

			Location location = attribute.GetLocation();
			string attributeName = attribute.Name.ToString();
			var diagnostic = Diagnostic.Create(
				Diagnostics.DangerousAttributesShouldBeAvoided,
				location,
				attributeName
			);

			context.ReportDiagnostic( diagnostic );
		}

	}

}
