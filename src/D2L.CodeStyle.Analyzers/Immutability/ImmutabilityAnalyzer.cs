using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Diagnostics.ImmutableClassIsnt );

		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			var inspector = new MutabilityInspector( new KnownImmutableTypes( context.Compilation.Assembly ) );

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

			// skip classes not marked immutable
			if( !inspector.IsTypeMarkedImmutable( symbol ) ) {
				return;
			}

			var flags = MutabilityInspectionFlags.Default 
				| MutabilityInspectionFlags.AllowUnsealed // `symbol` is the concrete type
				| MutabilityInspectionFlags.IgnoreImmutabilityAttribute; // we're _validating_ the attribute

			var mutabilityResult = inspector.InspectType( symbol, context.SemanticModel, flags );

			if( mutabilityResult.IsMutable ) {
				var reason = m_resultFormatter.Format( mutabilityResult );
				var location = GetLocationOfClassIdentifierAndGenericParameters( root );
				var diagnostic = Diagnostic.Create( 
					Diagnostics.ImmutableClassIsnt, 
					location, 
					reason 
				);
				context.ReportDiagnostic( diagnostic );
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
