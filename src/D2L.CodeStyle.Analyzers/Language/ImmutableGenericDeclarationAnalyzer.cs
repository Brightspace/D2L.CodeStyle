using System;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ImmutableGenericDeclarationAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.GenericArgumentTypeMustBeImmutable );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeNode( ctx ),
				SyntaxKind.GenericName );
		}

		private static void AnalyzeNode(
			SyntaxNodeAnalysisContext context
		) {
			var syntaxNode = context.Node as GenericNameSyntax;
			var hostTypeSymbolInfo = context.SemanticModel.GetSymbolInfo( syntaxNode );
			var hostTypeSymbol = hostTypeSymbolInfo.Symbol as INamedTypeSymbol;
			if (hostTypeSymbol == default) {
				return;
			}

			var typeArgumentNode = syntaxNode.TypeArgumentList;
			for( int index = 0; index < typeArgumentNode.Arguments.Count; index++ ) {
				var hostParameterSymbol = hostTypeSymbol.TypeParameters[index];
				var declarationScope = hostParameterSymbol.GetImmutabilityScope();

				var argumentSymbolInfo = context.SemanticModel.GetSymbolInfo( typeArgumentNode.Arguments[index] );
				var typeSymbol = argumentSymbolInfo.Symbol as ITypeSymbol;
				if( typeSymbol == default ) {
					continue;
				}

				ImmutabilityScope argumentScope = typeSymbol.GetImmutabilityScope();

				if( declarationScope == ImmutabilityScope.SelfAndChildren
					&& argumentScope != ImmutabilityScope.SelfAndChildren
				) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.GenericArgumentTypeMustBeImmutable,
						context.Node.GetLocation(),
						messageArgs: new object[] { typeSymbol.Name } ) );
				}
			}
		}
	}
}