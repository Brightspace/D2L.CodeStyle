using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal class D2LPageAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.D2LPageDerivedMustBePartial
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			INamedTypeSymbol? d2lPage = context.Compilation.GetTypeByMetadataName( "D2L.Web.D2LPage" );
			if( d2lPage == null ) {
				return;
			}

			context.RegisterSyntaxNodeAction( ctx => AnalyzeClassDefinition( ctx, d2lPage ), SyntaxKind.ClassDeclaration );
		}

		private static void AnalyzeClassDefinition( SyntaxNodeAnalysisContext context, INamedTypeSymbol d2lPage ) {
			ClassDeclarationSyntax? cds = context.Node as ClassDeclarationSyntax;
			if( cds == null ) {
				return;
			}

			INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol( cds );
			if( symbol == null ) {
				return;
			}

			INamedTypeSymbol? baseType = symbol.BaseType;

			bool inheritsD2LPage = false;
			while( baseType != null && baseType.SpecialType != SpecialType.System_Object ) {
				if( baseType.Equals( d2lPage, SymbolEqualityComparer.Default ) ) {
					inheritsD2LPage = true;
					break;
				}
				baseType = baseType.BaseType;
			}

			if( inheritsD2LPage && !cds.Modifiers.Any( m => m.IsKind( SyntaxKind.PartialKeyword ) ) ) {
				context.ReportDiagnostic( Diagnostics.D2LPageDerivedMustBePartial, symbol.Locations.First() );
			}
		}
	}
}
