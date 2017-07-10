using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Diagnostics.ImmutableClassIsnt );

		private readonly MutabilityInspector m_immutabilityInspector = new MutabilityInspector( KnownImmutableTypes.Default );
		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
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

			// skip classes not marked immutable
			if( !m_immutabilityInspector.IsTypeMarkedImmutable( symbol ) ) {
				return;
			}

			var flags = MutabilityInspectionFlags.Default 
				| MutabilityInspectionFlags.AllowUnsealed // `symbol` is the concrete type
				| MutabilityInspectionFlags.IgnoreImmutabilityAttribute; // we're _validating_ the attribute

			var mutabilityResult = m_immutabilityInspector.InspectType( symbol, context.Compilation.Assembly, flags );

			if( mutabilityResult.IsMutable ) {
				var reason = m_resultFormatter.Format( mutabilityResult );
				var diagnostic = Diagnostic.Create( Diagnostics.ImmutableClassIsnt, root.GetLocation(), reason );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}
