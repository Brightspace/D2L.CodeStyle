#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReadOnlyParameterAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReadOnlyParameterIsnt
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol readOnlyAttribute = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.ReadOnlyAttribute" );

			if( readOnlyAttribute == null ) {
				return;
			}

			context.RegisterOperationAction(
				ctx => AnalyzeMethodBodyOperation(
					ctx,
					readOnlyAttribute,
					(IMethodBodyBaseOperation)ctx.Operation
				),
				OperationKind.ConstructorBody,
				OperationKind.MethodBody
			);

			context.RegisterOperationAction(
				ctx => AnalyzeLocalFunctionOperation(
					ctx,
					readOnlyAttribute,
					(ILocalFunctionOperation)ctx.Operation
				),
				OperationKind.LocalFunction
			);
		}
		private static void AnalyzeMethodBodyOperation(
			OperationAnalysisContext ctx,
			INamedTypeSymbol readOnlyAttribute,
			IMethodBodyBaseOperation operation
		) => AnalyzeParameters(
			ctx,
			readOnlyAttribute,
			(IMethodSymbol)ctx.ContainingSymbol,
			operation.BlockBody ?? operation.ExpressionBody
		);

		private static void AnalyzeLocalFunctionOperation(
			OperationAnalysisContext ctx,
			INamedTypeSymbol readOnlyAttribute,
			ILocalFunctionOperation operation
		) {
			if( operation.Body is null ) {
				return;
			}

			AnalyzeParameters(
				ctx,
				readOnlyAttribute,
				operation.Symbol,
				operation.Body
			);
		}

		private static void AnalyzeParameters(
			OperationAnalysisContext ctx,
			INamedTypeSymbol readOnlyAttribute,
			IMethodSymbol method,
			IBlockOperation operation
		) {
			IParameterSymbol[] readOnlyParameters = method.Parameters.Where( p => IsMarkedReadOnly( readOnlyAttribute, p ) ).ToArray();

			if( readOnlyParameters.Length == 0 ) {
				return;
			}

			SyntaxNode nodeToAnalyze = operation.Syntax switch {
				ArrowExpressionClauseSyntax arrow => arrow.Expression,
				_ => operation.Syntax
			};
			DataFlowAnalysis dataflow = operation.SemanticModel.AnalyzeDataFlow( nodeToAnalyze );

			foreach( IParameterSymbol parameter in readOnlyParameters ) {
				Location parameterLocation = null;
				Location getLocation() {
					return parameterLocation ??= parameter.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken ).GetLocation();
				}

				if( parameter.RefKind != RefKind.None ) {
					/**
					 * public void Foo( [ReadOnly] in int foo )
					 * public void Foo( [ReadOnly] ref int foo )
					 * public void Foo( [ReadOnly] out int foo )
					 */
					ctx.ReportDiagnostic(
						Diagnostics.ReadOnlyParameterIsnt,
						getLocation(),
						messageArgs: new[] { "is an in/ref/out parameter" }
					);
				}

				if( dataflow.WrittenInside.Contains( parameter ) ) {
					/**
					 * public void Foo( [ReadOnly] int foo ) {
					 *   foo = 1; // write
					 *   void Inline() { foo = 1; // write }
					 *   var lambda = () => foo = 1; // write
					 *   SomeRefFunc( ref foo ); // pass by ref, potential for write
					 * }
					 */
					ctx.ReportDiagnostic(
						Diagnostics.ReadOnlyParameterIsnt,
						getLocation(),
						messageArgs: new[] { "is assigned to and/or passed by reference" }
					);
				}
			}
		}

		private static bool IsMarkedReadOnly(
			INamedTypeSymbol readOnlyAttribute,
			IParameterSymbol parameterSymbol
		) {
			foreach( AttributeData attribute in parameterSymbol.GetAttributes() ) {
				if( IsReadOnlyAttribute( readOnlyAttribute, attribute.AttributeClass ) ) {
					return true;
				}
			}

			return false;
		}

		private static bool IsReadOnlyAttribute(
			INamedTypeSymbol readOnlyAttribute,
			INamedTypeSymbol type
		) {
			if( type == null ) {
				return false;
			}

			if( type.Equals( readOnlyAttribute, SymbolEqualityComparer.Default ) ) {
				return true;
			}

			return IsReadOnlyAttribute( readOnlyAttribute, type.BaseType );
		}
	}
}
