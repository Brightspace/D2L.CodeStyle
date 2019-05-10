using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReadOnlyParameterAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReadOnlyParameterIsnt
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol readOnlyAttribute = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.ReadOnlyAttribute" );

			if( readOnlyAttribute == null ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeParameter(
					ctx: ctx,
					readOnlyAttribute: readOnlyAttribute,
					syntax: ctx.Node as ParameterSyntax
				),
				SyntaxKind.Parameter
			);
		}

		private static void AnalyzeParameter(
			SyntaxNodeAnalysisContext ctx,
			INamedTypeSymbol readOnlyAttribute,
			ParameterSyntax syntax
		) {
			SemanticModel model = ctx.SemanticModel;

			IParameterSymbol parameter = model.GetDeclaredSymbol( syntax, ctx.CancellationToken );

			if( !IsMarkedReadOnly( readOnlyAttribute, parameter ) ) {
				return;
			}

			if( parameter.RefKind != RefKind.None ) {
				/**
				 * public void Foo( [ReadOnly] in int foo )
				 * public void Foo( [ReadOnly] ref int foo )
				 * public void Foo( [ReadOnly] out int foo )
				 */
				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.ReadOnlyParameterIsnt,
					syntax.GetLocation(),
					"is an in/ref/out parameter"
				) );
			}

			IMethodSymbol method = parameter.ContainingSymbol as IMethodSymbol;
			BlockSyntax methodBody = ( method.DeclaringSyntaxReferences[0].GetSyntax( ctx.CancellationToken ) as BaseMethodDeclarationSyntax ).Body;

			if( methodBody == null ) {
				/**
				 * Method declaration on an interface.
				 */
				return;
			}

			DataFlowAnalysis dataflow = model.AnalyzeDataFlow( methodBody );
			if( dataflow.WrittenInside.Contains( parameter ) ) {
				/**
				 * public void Foo( [ReadOnly] int foo ) {
				 *   foo = 1; // write
				 *   void Inline() { foo = 1; // write }
				 *   var lambda = () => foo = 1; // write
				 *   SomeRefFunc( ref foo ); // pass by ref, potential for write
				 * }
				 */
				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.ReadOnlyParameterIsnt,
					syntax.GetLocation(),
					"is assigned to and/or passed by reference"
				) );
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

			if( type == readOnlyAttribute ) {
				return true;
			}

			return IsReadOnlyAttribute( readOnlyAttribute, type.BaseType );
		}
	}
}
