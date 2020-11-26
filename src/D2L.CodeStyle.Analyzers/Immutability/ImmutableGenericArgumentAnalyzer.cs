using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// Emit diagnostics for generic types that must be marked immutable but
	/// aren't.
	/// </summary>
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ImmutableGenericArgumentAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.GenericArgumentImmutableMustBeApplied );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSymbolAction(
				ctx => Analyze(
					ctx
				),
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

			foreach( ITypeSymbol argument in symbol.TypeArguments ) {

				ImmutabilityScope scope = argument.GetImmutabilityScope();
				if( scope == ImmutabilityScope.SelfAndChildren ) {
					continue;
				}

				if( ImmutableGenericArgument.BaseClassDemandsImmutability( symbol, argument )
					|| ImmutableGenericArgument.InterfacesDemandImmutability( symbol, argument )
					|| ImmutableGenericArgument.ConstraintsDemandImmutabliity( symbol, argument )
				) {
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.GenericArgumentImmutableMustBeApplied,
						argument.DeclaringSyntaxReferences
							.First()
							.GetSyntax()
							.GetLocation() ) );
				}
			}
		}
	}
}
