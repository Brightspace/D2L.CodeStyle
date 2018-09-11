using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {

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
				AnalyzeNode,
				SyntaxKind.GenericName );
		}

		private static void AnalyzeNode(
			SyntaxNodeAnalysisContext context
		) {
			var syntaxNode = context.Node as GenericNameSyntax;

			// Attributes are not allowed on local function parameters so we
			// have to ignore this node, otherwise we'll tell people to
			// annotate a declaration that is forbidden.
			if( syntaxNode.Ancestors().Any( a => a is LocalFunctionStatementSyntax ) ) {
				return;
			}

			SymbolInfo hostTypeSymbolInfo = context.SemanticModel.GetSymbolInfo( syntaxNode );
			var hostTypeSymbol = hostTypeSymbolInfo.Symbol as INamedTypeSymbol;
			if( hostTypeSymbol == default ) {
				return;
			}

			TypeArgumentListSyntax typeArgumentNode = syntaxNode.TypeArgumentList;
			for( int index = 0; index < typeArgumentNode.Arguments.Count; index++ ) {
				ITypeParameterSymbol hostParameterSymbol = hostTypeSymbol.TypeParameters[ index ];

				ImmutabilityScope declarationScope = hostParameterSymbol.GetImmutabilityScope();
				if( declarationScope != ImmutabilityScope.SelfAndChildren ) {
					continue;
				}

				SymbolInfo argumentSymbolInfo = context.SemanticModel.GetSymbolInfo( typeArgumentNode.Arguments[ index ] );
				var typeSymbol = argumentSymbolInfo.Symbol as ITypeSymbol;
				if( typeSymbol == default ) {
					continue;
				}

				ValidateImmutability(
					context,
					typeSymbol
				);
			}
		}

		private static void ValidateImmutability(
			SyntaxNodeAnalysisContext context,
			ITypeSymbol typeSymbol
		) {
			if( ImmutableContainerMethods.IsAnImmutableContainerType( typeSymbol ) ) {

				var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
				if( namedTypeSymbol == default( INamedTypeSymbol ) ) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.GenericArgumentTypeMustBeImmutable,
						context.Node.GetLocation(),
						messageArgs: new object[] { namedTypeSymbol.Name } ) );
				}

				foreach( var typeArgument in namedTypeSymbol.TypeArguments ) {

					ValidateImmutability( context, typeArgument );
				}
			} else {
				if( ( !KnownImmutableTypes.IsTypeKnownImmutable( typeSymbol ) ) &&
					( typeSymbol.GetImmutabilityScope() != ImmutabilityScope.SelfAndChildren ) ) {

					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.GenericArgumentTypeMustBeImmutable,
						context.Node.GetLocation(),
						messageArgs: new object[] { typeSymbol.Name } ) );
				}
			}
		}
	}
}
