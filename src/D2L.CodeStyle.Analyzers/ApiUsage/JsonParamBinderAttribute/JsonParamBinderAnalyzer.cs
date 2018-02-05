using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class JsonParamBinderAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ObsoleteJsonParamBinder );

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
				AnalyzeAttribute,
				SyntaxKind.Attribute
			);
		}

		private void AnalyzeAttribute( SyntaxNodeAnalysisContext context ) {

			AttributeSyntax attribute = context.Node as AttributeSyntax;
			if( attribute == null ) {
				return;
			}

			if( !attribute.Name.ToString().Equals( "JsonParamBinder" ) ) {
				return;
			}

			ISymbol methodSymbol = context.ContainingSymbol;
			if( methodSymbol.Kind != SymbolKind.Method ) {
				return;
			}

			ISymbol classSymbol = methodSymbol.ContainingSymbol;
			if( classSymbol.Kind != SymbolKind.NamedType ) {
				return;
			}

			string className = classSymbol.ToDisplayString( ClassDisplayFormat );
			if( LegacyJsonParamBinderClasses.Classes.Contains( className ) ) {
				return;
			}

			Location location = attribute.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(
				Diagnostics.ObsoleteJsonParamBinder,
				location
			);

			context.ReportDiagnostic( diagnostic );
		}

		private static readonly SymbolDisplayFormat ClassDisplayFormat = new SymbolDisplayFormat(
			memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
			localOptions: SymbolDisplayLocalOptions.IncludeType,
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
		);

	}

}
