using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzerTypeArgumentReport;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzerTypeArgumentSyntaxSniffer : DiagnosticAnalyzer {

		private static readonly string ReportName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.TypeArgumentLengthMismatch
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public void CompilationStart(
			CompilationStartAnalysisContext context
		) {

			ConcurrentBag<SimpleNameTuple> simpleNames = new();

			context.RegisterSyntaxNodeAction(
				ctx => {
					IdentifierNameSyntax identifierName = (IdentifierNameSyntax)ctx.Node;
					bool anazlye = ShouldAnalyzeTypeArguments( ctx, identifierName, out SymbolKind symbolKind );
					if( anazlye ) {
						simpleNames.Add( new( identifierName, symbolKind ) );
					}
				},
				SyntaxKind.IdentifierName
			);

			context.RegisterSyntaxNodeAction(
				ctx => {
					GenericNameSyntax genericName = (GenericNameSyntax)ctx.Node;
					bool anazlye = ShouldAnalyzeTypeArguments( ctx, genericName, out SymbolKind symbolKind );
					if( anazlye ) {
						simpleNames.Add( new( genericName, symbolKind ) );
					}
				},
				SyntaxKind.GenericName
			);

			context.RegisterCompilationEndAction(
				ctx => {
					WriteReports( ReportName, simpleNames );
				}
			);
		}

		private static bool ShouldAnalyzeTypeArguments(
			SyntaxNodeAnalysisContext ctx,
			SimpleNameSyntax syntax,
			out SymbolKind symbolKind
		) {
			if( syntax.IsFromDocComment() ) {
				// ignore things in doccomments such as crefs
				symbolKind = default;
				return false;
			}

			SymbolInfo info = ctx.SemanticModel.GetSymbolInfo( syntax, ctx.CancellationToken );

			ISymbol? symbol = info.Symbol;
			if( symbol == null ) {
				symbolKind = default;
				return false;
			}

			// Ignore anything that cannot have type arguments/parameters
			if( !GetTypeParamsAndArgs( symbol, out var typeParameters, out var typeArguments ) ) {
				symbolKind = default;
				return false;
			}

			if( typeParameters.IsEmpty && typeArguments.IsEmpty ) {
				symbolKind = default;
				return false;
			}

			if( typeParameters.Length != typeArguments.Length ) {

				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.TypeArgumentLengthMismatch,
					syntax.GetLocation()
				) );

				symbolKind = default;
				return false;
			}

			symbolKind = symbol.Kind;
			return true;
		}

		private static bool GetTypeParamsAndArgs( ISymbol type, out ImmutableArray<ITypeParameterSymbol> typeParameters, out ImmutableArray<ITypeSymbol> typeArguments ) {
			switch( type ) {
				case IMethodSymbol method:
					typeParameters = method.TypeParameters;
					typeArguments = method.TypeArguments;
					return true;
				case INamedTypeSymbol namedType:
					typeParameters = namedType.TypeParameters;
					typeArguments = namedType.TypeArguments;
					return true;
				default:
					return false;
			}
		}
	}
}
