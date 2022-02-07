using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class DangerousMemberUsagesAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousMethodsShouldBeAvoided,
			Diagnostics.DangerousPropertiesShouldBeAvoided
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			DangerousMembersModel? model = DangerousMembersModel.TryCreate( compilation );
			if( model == null ) {
				return;
			}

			if( model.HasMethods ) {

				context.RegisterOperationAction(
						context => {
							IInvocationOperation invocation = (IInvocationOperation)context.Operation;
							AnalyzeMethod( context, model, invocation.TargetMethod );
						},
						OperationKind.Invocation
					);

				context.RegisterOperationAction(
						context => {
							IMethodReferenceOperation operation = (IMethodReferenceOperation)context.Operation;
							AnalyzeMethod( context, model, operation.Method );
						},
						OperationKind.MethodReference
					);
			}

			if( model.HasProperties ) {

				context.RegisterOperationAction(
						context => {
							IPropertyReferenceOperation propertyReference = (IPropertyReferenceOperation)context.Operation;
							AnalyzeProperty( context, model, propertyReference.Property );
						},
						OperationKind.PropertyReference
					);
			}
		}

		private static void AnalyzeMethod(
				OperationAnalysisContext context,
				DangerousMembersModel model,
				IMethodSymbol method
			) {

			if( !model.IsDangerousMethod( context.ContainingSymbol, method ) ) {
				return;
			}

			ReportDiagnostic( context, method, Diagnostics.DangerousMethodsShouldBeAvoided );
		}

		private static void AnalyzeProperty(
				OperationAnalysisContext context,
				DangerousMembersModel model,
				IPropertySymbol property
			) {

			if( !model.IsDangerousProperty( context.ContainingSymbol, property ) ) {
				return;
			}

			ReportDiagnostic( context, property, Diagnostics.DangerousPropertiesShouldBeAvoided );
		}

		private static void ReportDiagnostic(
				OperationAnalysisContext context,
				ISymbol memberSymbol,
				DiagnosticDescriptor diagnosticDescriptor
			) {

			Location location = context.ContainingSymbol.Locations[ 0 ];
			string methodName = memberSymbol.ToDisplayString( MemberDisplayFormat );

			context.ReportDiagnostic(
					diagnosticDescriptor,
					location,
					messageArgs: new[] { methodName }
				);
		}

		private static readonly SymbolDisplayFormat MemberDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);
	}
}
