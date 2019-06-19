using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed partial class StructShouldBeReadonlyAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.StructShouldBeReadonly );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.None
			);

			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSymbolAction(
				Analyze,
				SymbolKind.NamedType
			);
		}

		private static void Analyze(
			SymbolAnalysisContext context
		) {
			var symbol = (INamedTypeSymbol)context.Symbol;

			if( !symbol.IsDefinition ) {
				return;
			}

			if( symbol.TypeKind != TypeKind.Struct ) {
				return;
			}

			if( symbol.IsImplicitlyDeclared ) {
				return;
			}

			ImmutableArray<SyntaxReference> declarationRefs = symbol.DeclaringSyntaxReferences;

			if( declarationRefs.Length == 0 ) {
				return;
			}

			// This foreach can be replaced if symbol.IsReadOnly when we update
			// to CodeAnalysis 3.1.0 (VS2019)
			foreach( SyntaxReference declarationRef in declarationRefs ) {
				StructDeclarationSyntax declaration = declarationRef
					.GetSyntax( context.CancellationToken ) as StructDeclarationSyntax;

				foreach( SyntaxToken token in declaration.Modifiers ) {
					if( token.IsKind( SyntaxKind.ReadOnlyKeyword ) ) {
						return;
					}
				}
			}

			if( !ShouldBeReadOnly( symbol ) ) {
				return;
			}

			StructDeclarationSyntax firstDeclaration = declarationRefs
				.First()
				.GetSyntax( context.CancellationToken ) as StructDeclarationSyntax;

			Location location = firstDeclaration
				.Identifier
				.GetLocation();

			context.ReportDiagnostic( Diagnostic.Create(
				Diagnostics.StructShouldBeReadonly,
				location,
				symbol.MetadataName
			) );
		}

		private static bool ShouldBeReadOnly( INamedTypeSymbol symbol ) {
			foreach( ISymbol member in symbol.GetMembers() ) {
				switch( member ) {
					case IFieldSymbol field:
						if( !( field.IsReadOnly || field.IsConst ) ) {
							return false;
						}
						break;

					case IPropertySymbol property:
						if( !property.IsReadOnly ) {
							return false;
						}
						break;
				}
			}

			return true;
		}
	}
}
