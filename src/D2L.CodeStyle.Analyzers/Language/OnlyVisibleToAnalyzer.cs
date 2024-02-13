using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed partial class OnlyVisibleToAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.MemberNotVisibleToCaller,
			Diagnostics.TypeNotVisibleToCaller
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		public static void OnCompilationStart( CompilationStartAnalysisContext context ) {

			Model? nullableModel = Model.TryCreate( context.Compilation );
			if( !nullableModel.HasValue ) {
				return;
			}

			Model model = nullableModel.Value;

			context.RegisterOperationAction(
				context => {
					IObjectCreationOperation creation = (IObjectCreationOperation)context.Operation;
					if( creation.Constructor != null ) {
						AnalyzeMemberUsage( context, creation.Constructor, model );
					}
				},
				OperationKind.ObjectCreation
			);

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

			context.RegisterSyntaxNodeAction(
				context => AnalyzeTypeUsage( context, (IdentifierNameSyntax)context.Node, model ),
				SyntaxKind.IdentifierName
			);
		}

		private static void AnalyzeMemberUsage(
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

		private static void AnalyzeTypeUsage(
			SyntaxNodeAnalysisContext context,
			IdentifierNameSyntax node,
			in Model model
		) {
			INamedTypeSymbol? caller = context.ContainingSymbol?.ContainingType;
			if( caller == null ) {
				return;
			}

			ISymbol? originalDefinition = context
				.SemanticModel
				.GetSymbolInfo( node )
				.Symbol?
				.OriginalDefinition;
			if( originalDefinition is not INamedTypeSymbol symbol ) {
				return;
			}

			if( symbol.TypeKind != TypeKind.Interface
				&& symbol.TypeKind != TypeKind.Class ) {
				return;
			}

			if( model.IsVisibleTo( caller, symbol ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
				descriptor: Diagnostics.TypeNotVisibleToCaller,
				location: node.Parent is QualifiedNameSyntax qns
					? qns.GetLocation()
					: node.GetLocation(),
				messageArgs: new[] {
					symbol.Name
				}
			);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
