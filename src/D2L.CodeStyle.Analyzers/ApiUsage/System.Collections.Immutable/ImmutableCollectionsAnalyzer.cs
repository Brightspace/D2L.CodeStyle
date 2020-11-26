using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.SystemCollectionsImmutable {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutableCollectionsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(
				Diagnostics.DontUseImmutableArrayConstructor
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );

		}
		public static void RegisterAnalysis(
			CompilationStartAnalysisContext context
		) {
			var immutableArrayType = context.Compilation
				.GetTypeByMetadataName( "System.Collections.Immutable.ImmutableArray`1" );

			// Bail early if we don't have a reference to ImmutableArray
			if( immutableArrayType == null ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeNewImmutableArray( ctx, immutableArrayType ),
				SyntaxKind.ObjectCreationExpression
			);
		}

		public static void AnalyzeNewImmutableArray(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol immutableArrayType
		) {
			var node = context.Node as ObjectCreationExpressionSyntax;

			// We're only concerned with the default (no arg) constructor for ImmutableArray
			if( node.ArgumentList != null && node.ArgumentList.Arguments.Count != 0 ) {
				return;
			}

			var specificType = context.SemanticModel.GetTypeInfo( context.Node ).Type
				as INamedTypeSymbol;

			// This happens for generic types
			if ( specificType == null ) {
				return;
			}

			// We only care about ImmutableArray`1
			if( !specificType.OriginalDefinition.Equals( immutableArrayType, SymbolEqualityComparer.Default ) ) {
				return;
			}

			// All usages are bad
			context.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.DontUseImmutableArrayConstructor,
					node.GetLocation()
				)
			);
		}
	}
}
