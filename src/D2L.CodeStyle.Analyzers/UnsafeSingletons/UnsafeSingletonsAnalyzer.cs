using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.UnsafeSingletons {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class UnsafeSingletonsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Diagnostics.UnsafeSingletonField );

		private readonly MutabilityInspector m_immutabilityInspector = new MutabilityInspector( KnownImmutableTypes.Default );
		private readonly Utils m_utils = new Utils();
		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			context.RegisterSyntaxNodeAction( AnalyzeClass, SyntaxKind.ClassDeclaration );
		}

		private void AnalyzeClass( SyntaxNodeAnalysisContext context ) {
			if( m_utils.IsGeneratedCodefile( context.Node.SyntaxTree.FilePath ) ) {
				// skip code-gen'd files; they have been hand-inspected to be safe
				return;
			}

			var root = context.Node as ClassDeclarationSyntax;
			if( root == null ) {
				return;
			}

			var type = context.SemanticModel.GetDeclaredSymbol( root );
			if( type == null || type.TypeKind == TypeKind.Error ) {
				return;
			}

			if( !IsTypeSingleton( type ) ) {
				return;
			}

			// TODO: it probably makes more sense to iterate over the fields and emit diagnostics tied to those individual fields for more accurate red-squigglies
			// a DI singleton should be capable of having multiple diagnostics come out of it
			var flags = MutabilityInspectionFlags.AllowUnsealed | MutabilityInspectionFlags.IgnoreImmutabilityAttribute;
			var result = m_immutabilityInspector.InspectType( type, context.Compilation.Assembly, flags );
			if( result.IsMutable ) {
				var diagnostic = CreateDiagnostic(
					root.GetLocation(),
					type.GetFullTypeNameWithGenericArguments(),
					result
				);
				context.ReportDiagnostic( diagnostic );
			}
		}

		private bool IsTypeSingleton( INamedTypeSymbol type ) {
			// TODO: implement this method when we have that information.
			return false;
		}

		private Diagnostic CreateDiagnostic(
			Location location,
			string typeName,
			MutabilityInspectionResult result
		) {
			var reason = m_resultFormatter.Format( result );
			var diagnostic = Diagnostic.Create(
				Diagnostics.UnsafeSingletonField,
				location,
				typeName,
				reason
			);
			return diagnostic;
		}

	}
}
