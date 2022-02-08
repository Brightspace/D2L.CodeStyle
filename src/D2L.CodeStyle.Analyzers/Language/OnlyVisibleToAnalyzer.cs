using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed partial class OnlyVisibleToAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.MemberNotVisibleToCaller
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		public void OnCompilationStart( CompilationStartAnalysisContext context ) {

			Model? nullableModel = Model.TryCreate( context.Compilation );
			if( !nullableModel.HasValue ) {
				return;
			}

			Model model = nullableModel.Value;

			context.RegisterOperationAction(
				context => {
					IInvocationOperation invocation = (IInvocationOperation)context.Operation;
					AnalyzeMemberUsage( context, invocation.TargetMethod, model );
				},
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				context => {
					IMethodReferenceOperation reference = (IMethodReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Method, model );
				},
				OperationKind.MethodReference
			);

			context.RegisterOperationAction(
				context => {
					IPropertyReferenceOperation reference = (IPropertyReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Property, model );
				},
				OperationKind.PropertyReference
			);
		}

		private void AnalyzeMemberUsage(
			OperationAnalysisContext context,
			ISymbol member,
			in Model model
		) {

			INamedTypeSymbol caller = context.ContainingSymbol.ContainingType;
			if( model.IsVisibleTo( caller, member ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
				Diagnostics.MemberNotVisibleToCaller,
				context.Operation.Syntax.GetLocation(),
				messageArgs: new[] {
					member.Name
				}
			);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
