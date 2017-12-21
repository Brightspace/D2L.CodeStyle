using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.LaunchDarkly {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ObsoleteLaunchDarklyApisAnalyzer : DiagnosticAnalyzer {

		public const string IFeatureFullName = "D2L.LP.LaunchDarkly.FeatureFlagging.IFeature";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ObsoleteLaunchDarklyFramework
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			context.RegisterSyntaxNodeAction(
					AnalyzeSimpleBaseType,
					SyntaxKind.SimpleBaseType
				);
		}

		private void AnalyzeSimpleBaseType(
				SyntaxNodeAnalysisContext context
			) {

			SimpleBaseTypeSyntax baseTypeSyntax = (SimpleBaseTypeSyntax)context.Node;

			SymbolInfo baseTypeSymbol = context.SemanticModel.GetSymbolInfo( baseTypeSyntax.Type );
			if( baseTypeSymbol.Symbol == null ) {
				return;
			}

			string baseTypeFullName = baseTypeSymbol.Symbol.ToDisplayString();
			if( !baseTypeFullName.Equals( IFeatureFullName ) ) {
				return;
			}

			SyntaxNode classNode = baseTypeSyntax.Parent.Parent;

			ISymbol featureSymbol = context.SemanticModel.GetDeclaredSymbol( classNode );
			if( featureSymbol == null ) {
				return;
			}

			string featureName = featureSymbol.ToDisplayString();
			if( LegacyFeatureTypes.Types.Contains( featureName ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.ObsoleteLaunchDarklyFramework,
					baseTypeSyntax.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
