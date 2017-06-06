using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzer : DiagnosticAnalyzer {

		public const string DiagnosticId = "D2L0003";
		private const string Category = "Safety";

		private const string Title = "Classes marked as immutable should be immutable.";
		private const string Description = "Classes marked as immutable or that implement interfaces marked immutable should be immutable.";
		internal const string MessageFormat = "This class is marked immutable, but it is not. Check that all fields and properties are immutable.";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId, 
			Title, 
			MessageFormat, 
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

		private readonly MutabilityInspector m_immutabilityInspector = new MutabilityInspector();

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationStartAction( ctx =>
				ctx.RegisterSyntaxNodeAction(
					AnalyzeClass,
					SyntaxKind.ClassDeclaration
				)
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

			if( m_immutabilityInspector.InspectType( symbol, flags ).IsMutable ) {
				var diagnostic = Diagnostic.Create( Rule, root.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}

	}
}
