using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ClassShouldBeSealed {
	/// <summary>
	/// Emit diagnostics for internal or private types that could be sealed but
	/// aren't.
	/// </summary>
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ClassShouldBeSealedAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ClassShouldBeSealed );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			var privateOrInternalBaseClasses = new ConcurrentDictionary<INamedTypeSymbol, bool>();
			var privateOrInternalUnsealedClasses = new ConcurrentDictionary<INamedTypeSymbol, Location>();

			context.RegisterSymbolAction(
				ctx => Collect(
					ctx,
					privateOrInternalBaseClasses,
					privateOrInternalUnsealedClasses
				),
				SymbolKind.NamedType
			);

			context.RegisterCompilationEndAction(
				ctx => EmitDiagnostics(
					ctx,
					privateOrInternalBaseClasses,
					privateOrInternalUnsealedClasses
				)
			);
		}

		private static void Collect(
			SymbolAnalysisContext context,
			ConcurrentDictionary<INamedTypeSymbol, bool> privateOrInternalBaseClasses,
			ConcurrentDictionary<INamedTypeSymbol, Location> privateOrInternalUnsealedClasses
		) {
			var symbol = (INamedTypeSymbol)context.Symbol;

			if ( symbol.BaseType != null ) {
				privateOrInternalBaseClasses[symbol.BaseType] = true;
			}

			if ( !symbol.IsDefinition ) {
				return;
			}

			if ( symbol.IsSealed ) {
				return;
			}

			// An abstract class can't be sealed. If this analyzer would emit a
			// diagnostic then this class is probably dead-code. That's worth
			// complaining about but not in this analyzer.
			if ( symbol.IsAbstract ) {
				return;
			}

			if ( symbol.DeclaredAccessibility.HasFlag( Accessibility.Public ) ) {
				return;
			}

			if ( symbol.DeclaringSyntaxReferences.Length == 0 ) {
				return;
			}

			var firstDecl = symbol
				.DeclaringSyntaxReferences
				.First()
				.GetSyntax();

			if ( firstDecl is ClassDeclarationSyntax ) {
				// at this point we know its a class, its private or internal and its not sealed
				privateOrInternalUnsealedClasses[symbol] = (firstDecl as ClassDeclarationSyntax).Identifier.GetLocation();
			}
		}

		private static void EmitDiagnostics(
			CompilationAnalysisContext context,
			ConcurrentDictionary<INamedTypeSymbol, bool> privateOrInternalBaseClasses,
			ConcurrentDictionary<INamedTypeSymbol, Location> privateOrInternalUnsealedClasses
		) {
			foreach( var unsealed in privateOrInternalUnsealedClasses ) {
				if( !privateOrInternalBaseClasses.ContainsKey( unsealed.Key ) ) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.ClassShouldBeSealed,
						unsealed.Value
					) );
				}
			}
		}
	}
}
