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
						IInvocationOperation invocation = (IInvocationOperation)context.Operation;
						AnalyzeInvocation( context, invocation, cancellationTokenType );
					},
					OperationKind.Invocation
				);
		}

		private static void AnalyzeInvocation(
				OperationAnalysisContext context,
				IInvocationOperation invocation,
				INamedTypeSymbol cancellationTokenType
			) {

			ImmutableArray<IArgumentOperation> arguments = invocation.Arguments;
			foreach( IArgumentOperation argument in arguments ) {

				IParameterSymbol? parameter = argument.Parameter;
				if( parameter.IsNullOrErrorType() ) {
					continue;
				}

				if( !parameter.HasExplicitDefaultValue ) {
					continue;
				}

				if( !SymbolEqualityComparer.Default.Equals( parameter.Type, cancellationTokenType ) ) {
					continue;
				}

				if( argument.ArgumentKind != ArgumentKind.DefaultValue ) {
					continue;
				}

				InvocationExpressionSyntax syntax = (InvocationExpressionSyntax)invocation.Syntax;

				context.ReportDiagnostic(
						Diagnostics.ExplicitCancellationTokenArgumentRequired,
						syntax.Expression.GetLocation()
					);
			}
		}
	}
}
