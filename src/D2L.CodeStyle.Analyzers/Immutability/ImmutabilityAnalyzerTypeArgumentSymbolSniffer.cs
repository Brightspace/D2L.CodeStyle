using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzerTypeArgumentReport;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzerTypeArgumentSymbolSniffer : DiagnosticAnalyzer {

		private static readonly string ReportName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.TypeArgumentLengthMismatch
			);

		public override void Initialize( AnalysisContext context ) {
			//Debugger.Launch();
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public void CompilationStart( CompilationStartAnalysisContext context ) {

			ConcurrentBag<SimpleNameTuple> simpleNames = new();

			context.RegisterSymbolAction(
				ctx => {
					INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Symbol;
					AnalyzeNamedType( ctx, namedType, simpleNames );
				},
				SymbolKind.NamedType
			);

			context.RegisterCompilationEndAction(
				ctx => {
					WriteReports( ReportName, simpleNames );
				}
			);
		}

		private static void AnalyzeNamedType(
			SymbolAnalysisContext ctx,
			INamedTypeSymbol namedType,
			ConcurrentBag<SimpleNameTuple> simpleNames
		) {

			ImmutableHashSet<ITypeSymbol> baseTypeSymbols =
				GetBaseAndInterfaceTypes( namedType )
				.Where( baseType => ShouldAnalyzeNamedTypeReference( ctx, baseType ) )
				.ToImmutableHashSet<ITypeSymbol>( SymbolEqualityComparer.Default );

			if( baseTypeSymbols.Count == 0 ) {
				return;
			}

			ImmutableArray<SyntaxReference> syntaxReferences = namedType.DeclaringSyntaxReferences;
			foreach( SyntaxReference syntaxReference in syntaxReferences ) {

				SyntaxNode syntax = syntaxReference.GetSyntax( ctx.CancellationToken );
				if( syntax is not TypeDeclarationSyntax typeDeclaration ) {
					throw new InvalidOperationException( "INamedTypeSymbol should have referenced TypeDeclarationSyntax" );
				}

				BaseListSyntax? baseList = typeDeclaration.BaseList;
				if( baseList == null ) {
					continue;
				}

				SemanticModel model = ctx.Compilation.GetSemanticModel( typeDeclaration.SyntaxTree );

				foreach( BaseTypeSyntax baseType in baseList.Types ) {

					if( baseType.Type.IsKind( SyntaxKind.ErrorKeyword ) ) {
						continue;
					}

					if( baseType.Type is not NameSyntax baseTypeNameSyntax ) {
						throw new InvalidOperationException( "The BaseTypeSyntax's Type should be a NameSyntax" );
					}

					ITypeSymbol? baseTypeSymbol = model
						.GetTypeInfo( baseType.Type, ctx.CancellationToken )
						.Type;

					if( baseTypeSymbol == null ) {
						continue;
					}

					if( !baseTypeSymbols.Contains( baseTypeSymbol ) ) {
						continue;
					}

					SimpleNameSyntax baseTypeUnqualifiedName = baseTypeNameSyntax.GetUnqualifiedName();
					simpleNames.Add( new( baseTypeUnqualifiedName, baseTypeSymbol.Kind ) );
				}
			}
		}

		private static IEnumerable<INamedTypeSymbol> GetBaseAndInterfaceTypes( ITypeSymbol namedType ) {

			INamedTypeSymbol? baseType = namedType.BaseType;
			if( baseType != null ) {
				yield return baseType;
			}

			foreach( INamedTypeSymbol interfaceType in namedType.Interfaces ) {
				yield return interfaceType;
			}
		}

		private static bool ShouldAnalyzeNamedTypeReference(
				SymbolAnalysisContext ctx,
				INamedTypeSymbol namedType
			) {

			ImmutableArray<ITypeParameterSymbol> typeParameters = namedType.TypeParameters;
			ImmutableArray<ITypeSymbol> typeArguments = namedType.TypeArguments;

			if( typeParameters.IsEmpty && typeArguments.IsEmpty ) {
				return false;
			}

			if( typeParameters.Length != typeArguments.Length ) {

				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.TypeArgumentLengthMismatch,
					namedType.Locations[ 0 ]
				) );

				return false;
			}

			return true;
		}
	}
}
