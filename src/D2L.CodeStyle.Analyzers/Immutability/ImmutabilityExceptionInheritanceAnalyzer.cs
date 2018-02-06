using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityExceptionInheritanceAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ImmutableExceptionInheritanceIsInvalid
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
				AnalyzeTypeDeclaration,
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.StructDeclaration
			);
		}

		private void AnalyzeTypeDeclaration( SyntaxNodeAnalysisContext context ) {

			// TypeDeclarationSyntax is the base class of
			// ClassDeclarationSyntax and StructDeclarationSyntax
			var root = (TypeDeclarationSyntax)context.Node;

			var symbol = context.SemanticModel
				.GetDeclaredSymbol( root );

			ImmutableHashSet<string> directExceptions;
			if( !symbol.TryGetDirectImmutableExceptions( out directExceptions ) ) {
				// Type has no direct exceptions defined, so there's nothing to analyze
				return;
			}

			var allInheritedExceptions = symbol.GetInheritedImmutableExceptions();

			foreach( KeyValuePair<ISymbol, ImmutableHashSet<string>> inheritedExceptions in allInheritedExceptions ) {
				
				if( !directExceptions.IsSubsetOf( inheritedExceptions.Value ) ) {

					var suggestedFix = inheritedExceptions.Value.IsEmpty
						? "Set the [Immutable] exceptions on this type to Except.None."
						: $"Reduce the [Immutable] exceptions on this type to a subset of {{ {string.Join( ", ", inheritedExceptions.Value )} }}.";

					var location = GetLocationOfClassIdentifierAndGenericParameters( root );
					var diagnostic = Diagnostic.Create(
						Diagnostics.ImmutableExceptionInheritanceIsInvalid,
						location,
						inheritedExceptions.Key.Name,
						suggestedFix
					);

					context.ReportDiagnostic( diagnostic );
				}
			}
		}

		private Location GetLocationOfClassIdentifierAndGenericParameters(
			TypeDeclarationSyntax decl
		) {
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
