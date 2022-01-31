using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class AttributeUnqualifiedNameAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ConciseAttributeName
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private void CompilationStart( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttribute( c, (AttributeSyntax)c.Node ),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttribute(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attribute
			) {

			string unqualifiedName = attribute.Name
				.GetUnqualifiedName()
				.ToString();

			if( !unqualifiedName.EndsWith( "Attribute" ) ) {
				return;
			}

			Diagnostic d = Diagnostic.Create(
					Diagnostics.ConciseAttributeName,
					attribute.GetLocation()
				);

			context.ReportDiagnostic( d );
		}
	}
}
