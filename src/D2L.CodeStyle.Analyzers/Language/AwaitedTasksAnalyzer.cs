#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed partial class AwaitedTasksAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.AwaitedTaskNotConfigured );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.None
			);

			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {

			ImmutableHashSet<ITypeSymbol> configuredTaskTypes =
				new[] {
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" ),
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1" ),
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable" ),
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1" )
				}
				.Where( x => x != null && x.Kind != SymbolKind.ErrorType )
				.ToImmutableHashSet<ITypeSymbol>( SymbolEqualityComparer.Default );

			if( configuredTaskTypes.IsEmpty ) {
				return;
			}

			context.RegisterOperationAction(
				ctx => AnalyzeAwait(
					context: ctx,
					configuredTaskTypes: configuredTaskTypes,
					operation: ctx.Operation as IAwaitOperation
				),
				OperationKind.Await
			);
		}

		private static void AnalyzeAwait(
			OperationAnalysisContext context,
			ImmutableHashSet<ITypeSymbol> configuredTaskTypes,
			IAwaitOperation operation
		) {
			IOperation rhs = operation.Operation;
			ITypeSymbol awaitedType = rhs.Type.OriginalDefinition;

			if( configuredTaskTypes.Contains( awaitedType ) ) {
				return;
			}

			context.ReportDiagnostic( Diagnostic.Create(
				Diagnostics.AwaitedTaskNotConfigured,
				operation.Syntax.GetLocation()
			) );
		}
	}
}
