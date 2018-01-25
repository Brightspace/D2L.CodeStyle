using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class UnsafeSingletonsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.SingletonIsntImmutable 
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				AnalyzeClass,
				SyntaxKind.ClassDeclaration
			);
		}

		private void AnalyzeClass( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as ClassDeclarationSyntax;
			if( root == null ) {
				return;
			}

			var symbol = context.SemanticModel.GetDeclaredSymbol( root );
			if( symbol == null ) {
				return;
			}

			// skip classes not marked singleton
			if( !symbol.IsTypeMarkedSingleton() ) {
				return;
			}

			var isMarkedImmutable = symbol.IsTypeMarkedImmutable();
			if( !isMarkedImmutable ) {
				var location = GetLocationOfClassIdentifierAndGenericParameters( root );
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.SingletonIsntImmutable,
					location
				) );
			}
		}


		private Location GetLocationOfClassIdentifierAndGenericParameters( ClassDeclarationSyntax decl ) {
			var location = decl.Identifier.GetLocation();

			if( decl.TypeParameterList != null ) {
				location = Location.Create(
					decl.SyntaxTree,
					TextSpan.FromBounds(
						location.SourceSpan.Start,
						decl.TypeParameterList.GetLocation().SourceSpan.End
					)
				);
			}

			return location;
		}
	}
}
