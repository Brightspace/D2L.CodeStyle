using System;
using System.Collections.Immutable;
using System.Linq;
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
			var knownImmutableTypes = new KnownImmutableTypes( context.Compilation.Assembly );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeNode( 
					ctx, 
					knownImmutableTypes ),
				SyntaxKind.GenericName );
		}

		private static void AnalyzeNode(
			SyntaxNodeAnalysisContext context,
			KnownImmutableTypes knownImmutableTypes
		) {
			var syntaxNode = context.Node as GenericNameSyntax;

			// Attributes are not allowed on local function parameters so we
			// have to ignore this node, otherwise we'll tell people to
			// annotate a declaration that is forbidden.
			if( syntaxNode.Ancestors().Any( a => a is LocalFunctionStatementSyntax )) {
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

				var typeName = typeSymbol.GetFullTypeName();
				if( typeName == "System.Nullable" ) {
					var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
					if (namedTypeSymbol == default(INamedTypeSymbol)) {
						continue;
					}
					typeSymbol = namedTypeSymbol.TypeArguments[ 0 ];
				}

				if( knownImmutableTypes.IsTypeKnownImmutable( typeSymbol ) ) {
					continue;
				}

				ImmutabilityScope argumentScope = typeSymbol.GetImmutabilityScope();
				if( argumentScope != ImmutabilityScope.SelfAndChildren ) {

					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.GenericArgumentTypeMustBeImmutable,
						context.Node.GetLocation(),
						messageArgs: new object[] { typeSymbol.Name } ) );
				}
			}
		}
	}
}
