using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static D2L.CodeStyle.Analyzers.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DataRecordConverters {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class InterfaceBinderAnalyzer : DiagnosticAnalyzer {

		private const string InterfaceBinderFullName = "D2L.LP.LayeredArch.Data.InterfaceBinder`1";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			InterfaceBinder_InterfacesOnly
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private void CompilationStart( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( InterfaceBinderFullName, out INamedTypeSymbol? interfaceBinderType ) ) {
				return;
			}

			context.RegisterOperationAction(
				ctx => AnalyzeMemberReference(
					ctx,
					interfaceBinderType: interfaceBinderType,
					member: ( (IPropertyReferenceOperation)ctx.Operation ).Member
				),
				OperationKind.PropertyReference
			);

			context.RegisterOperationAction(
				ctx => AnalyzeMemberReference(
					ctx,
					interfaceBinderType: interfaceBinderType,
					member: ( (IInvocationOperation)ctx.Operation ).TargetMethod
				),
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				ctx => AnalyzeMemberReference(
					ctx,
					interfaceBinderType: interfaceBinderType,
					member: ( (IMethodReferenceOperation)ctx.Operation ).Member
				),
				OperationKind.MethodReference
			);
		}

		private static void AnalyzeMemberReference(
			OperationAnalysisContext context,
			INamedTypeSymbol interfaceBinderType,
			ISymbol member
		) {
			if( member.DeclaredAccessibility != Accessibility.Public ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( interfaceBinderType, member.ContainingType.OriginalDefinition ) ) {
				return;
			}

			ITypeSymbol boundType = member.ContainingType.TypeArguments[ 0 ];
			if( boundType.TypeKind == TypeKind.Interface ) {
				return;
			}

			context.ReportDiagnostic(
				InterfaceBinder_InterfacesOnly,
				context.Operation.Syntax.GetLocation(),
				messageArgs: new[] { boundType.Name }
			);
		}
	}
}
