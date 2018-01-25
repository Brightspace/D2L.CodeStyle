using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
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
			// There is no ConcurrentHashSet<T>, so use a dummy bool
			var privateOrInternalBaseClasses = new ConcurrentDictionary<INamedTypeSymbol, bool>();
			var privateOrInternalUnsealedClasses = new ConcurrentDictionary<INamedTypeSymbol, Location>();

			// During symbol action execution (and syntax node) we can't cheaply
			// (as far as I know) answer "are there any subtypes of T?", so
			// instead we keep track of all types that are used as a base type
			// and all class types that are unsealed.

			context.RegisterSymbolAction(
				ctx => Collect(
					ctx,
					privateOrInternalBaseClasses,
					privateOrInternalUnsealedClasses
				),
				SymbolKind.NamedType
			);

			// Afterwards, during our compilation end action we can compare the
			// two lists. Compilation end always occurs after all symbol actions
			// are complete. Documentation for ordering of actions can be read
			// here: https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md

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

			if ( !symbol.IsDefinition ) {
				return;
			}

			// We can ignore types in other assemblies (forgetting about
			// InternalsVisibleTo because if we're seeing them here they must
			// be public so 1) not our deal 2) not subtypes of our internal
			// or private types. This isn't strictly necessary because of
			// the next check (ignore public types) but it is more explicit.
			if ( symbol.ContainingAssembly != context.Compilation.Assembly ) {
				return;
			}

			// We can't make calls about public unsealed types and a public
			// type can't have an internal or private base type so we can
			// safely ignore them.
			if ( symbol.DeclaredAccessibility.HasFlag( Accessibility.Public ) ) {
				return;
			}

			if ( symbol.BaseType != null ) {
				privateOrInternalBaseClasses[symbol.BaseType] = true;
			}

			// From this point we are trying to determine if symbol represents
			// an internal or private unsealed type

			if ( symbol.IsStatic ) {
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

			// Not sure how this would happen... implicit types?
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
