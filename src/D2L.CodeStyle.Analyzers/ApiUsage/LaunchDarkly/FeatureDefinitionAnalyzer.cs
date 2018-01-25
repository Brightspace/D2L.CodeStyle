using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class FeatureDefinitionAnalyzer : DiagnosticAnalyzer {

		public const string FeatureDefinitionFullName = "D2L.LP.LaunchDarkly.FeatureDefinition`1";

		private static readonly ImmutableHashSet<string> ValidTypes = ImmutableHashSet.Create<string>(
				"int",
				"bool",
				"string",
				"float"
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.InvalidLaunchDarklyFeatureDefinition
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol featureDefinitionType = compilation.GetTypeByMetadataName( FeatureDefinitionFullName );
			if( featureDefinitionType.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeSimpleBaseType( c, featureDefinitionType ),
					SyntaxKind.SimpleBaseType
				);
		}

		private void AnalyzeSimpleBaseType(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol featureDefinitionType
			) {

			SimpleBaseTypeSyntax baseTypeSyntax = (SimpleBaseTypeSyntax)context.Node;
			SymbolInfo baseTypeSymbol = context.SemanticModel.GetSymbolInfo( baseTypeSyntax.Type );

			INamedTypeSymbol baseSymbol = ( baseTypeSymbol.Symbol as INamedTypeSymbol );
			if( baseSymbol.IsNullOrErrorType() ) {
				return;
			}

			ISymbol originalSymbol = baseSymbol.OriginalDefinition;
			if( originalSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !originalSymbol.Equals( featureDefinitionType ) ) {
				return;
			}

			ISymbol valueTypeSymbol = baseSymbol.TypeArguments[ 0 ];
			if( valueTypeSymbol.IsNullOrErrorType() ) {
				return;
			}

			string valueType = valueTypeSymbol.ToDisplayString();
			if( ValidTypes.Contains( valueType ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.InvalidLaunchDarklyFeatureDefinition,
					baseTypeSyntax.GetLocation(),
					valueType
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
