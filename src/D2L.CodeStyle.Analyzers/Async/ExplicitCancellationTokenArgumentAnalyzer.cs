using System.Collections.Immutable;
using System.Diagnostics;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Async {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ExplicitCancellationTokenArgumentAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ExplicitCancellationTokenArgumentRequired
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			if( !compilation.TryGetTypeByMetadataName(
					"System.Threading.CancellationToken",
					out INamedTypeSymbol? cancellationTokenType
				) ) {
				return;
			}

			context.RegisterOperationAction(
					context => {
						IArgumentOperation argument = (IArgumentOperation)context.Operation;
						AnalyzeArgument( context, argument, cancellationTokenType );
					},
					OperationKind.Argument
				);
		}

		private static void AnalyzeArgument(
				OperationAnalysisContext context,
				IArgumentOperation argument,
				INamedTypeSymbol cancellationTokenType
			) {

			IParameterSymbol? parameter = argument.Parameter;
			if( parameter.IsNullOrErrorType() ) {
				return;
			}

			if( !parameter.HasExplicitDefaultValue ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( parameter.Type, cancellationTokenType ) ) {
				return;
			}

			if( argument.ArgumentKind != ArgumentKind.DefaultValue ) {
				return;
			}

			context.ReportDiagnostic(
					Diagnostics.ExplicitCancellationTokenArgumentRequired,
					argument.Syntax.GetLocation()
				);
		}
	}
}
