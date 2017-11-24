using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.UnsafeSingletons {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class UnsafeSingletonsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.SingletonIsntImmutable 
		);

		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			var inspector = new MutabilityInspector(
				context.Compilation,
				new KnownImmutableTypes( context.Compilation.Assembly )
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeClass( ctx, inspector ),
				SyntaxKind.ClassDeclaration
			);
		}

		private void AnalyzeClass( SyntaxNodeAnalysisContext context, MutabilityInspector inspector ) {
			var root = context.Node as ClassDeclarationSyntax;
			if( root == null ) {
				return;
			}

			var symbol = context.SemanticModel.GetDeclaredSymbol( root );
			if( symbol == null ) {
				return;
			}

			// skip classes not marked singleton
			if( !inspector.IsTypeMarkedSingleton( symbol ) ) {
				return;
			}

			var isMarkedImmutable = inspector.IsTypeMarkedImmutable( symbol );
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
