using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class FeatureDefinitionAnalyzer : DiagnosticAnalyzer {

		public const string FeatureDefinitionFullName = "D2L.LP.LaunchDarkly.FeatureDefinition`1";

		private static readonly ImmutableHashSet<SpecialType> ValidTypes = ImmutableHashSet.Create(
				SpecialType.System_Int32,
				SpecialType.System_Boolean,
				SpecialType.System_String,
				SpecialType.System_Single
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.InvalidLaunchDarklyFeatureDefinition
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol? featureDefinitionType = compilation.GetTypeByMetadataName( FeatureDefinitionFullName );
			if( featureDefinitionType.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSymbolAction(
				c => AnalyzeMaybeSubtype( c, featureDefinitionType ),
				SymbolKind.NamedType
			);
		}

		private static void AnalyzeMaybeSubtype(
				SymbolAnalysisContext context,
				INamedTypeSymbol featureDefinitionType
			) {

			INamedTypeSymbol maybeSubtype = (INamedTypeSymbol)context.Symbol;

			if( maybeSubtype.BaseType is null ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( maybeSubtype.BaseType.OriginalDefinition, featureDefinitionType ) ) {
				return;
			}

			ISymbol valueTypeSymbol = maybeSubtype.BaseType.TypeArguments[ 0 ];

			if( valueTypeSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( valueTypeSymbol is not INamedTypeSymbol namedValueType ) {
				return;
			}

			if( ValidTypes.Contains( namedValueType.SpecialType ) ) {
				return;
			}

			var (typeSyntax, baseTypeSyntax) = maybeSubtype.ExpensiveGetSyntaxImplementingType( maybeSubtype.BaseType, context.Compilation, context.CancellationToken );

			context.ReportDiagnostic(
					Diagnostics.InvalidLaunchDarklyFeatureDefinition,
					location: baseTypeSyntax?.GetLocation() ?? typeSyntax?.Identifier.GetLocation() ?? Location.None,
					messageArgs: new[] { namedValueType.ToDisplayString() }
				);
		}
	}
}
