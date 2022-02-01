using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReadOnlyParameterAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReadOnlyParameterIsnt
		);

		private const string FullyQualifiedAttributeName = "D2L.CodeStyle.Annotations.ReadOnlyAttribute";
		private static readonly ImmutableArray<string> UnqualifiedAttributeNames = ImmutableArray.Create( "ReadOnly", "ReadOnlyAttribute" );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol? readOnlyAttribute = compilation.GetTypeByMetadataName( FullyQualifiedAttributeName );
			if( readOnlyAttribute == null ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeParameter(
					ctx: ctx,
					readOnlyAttribute: readOnlyAttribute,
					syntax: (ParameterSyntax)ctx.Node
				),
				SyntaxKind.Parameter
			);
		}

		private static void AnalyzeParameter(
			SyntaxNodeAnalysisContext ctx,
			INamedTypeSymbol readOnlyAttribute,
			ParameterSyntax syntax
		) {

			if( !HasOnlyAttributeCandidates( syntax ) ) {
				return;
			}

			SemanticModel model = ctx.SemanticModel;

			IParameterSymbol? parameter = model.GetDeclaredSymbol( syntax, ctx.CancellationToken );
			if( parameter.IsNullOrErrorType() ) {
				return;
			}

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

			IMethodSymbol? method = parameter.ContainingSymbol as IMethodSymbol;
			if( method == null ) {
				return;
			}

			BaseMethodDeclarationSyntax? methodDeclaration = method.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken ) as BaseMethodDeclarationSyntax;
			if( methodDeclaration == null ) {
				return;
			}

			BlockSyntax? methodBody = methodDeclaration.Body;
			if( methodBody == null ) {
				/**
				 * Method declaration on an interface.
				 */
				return;
			}

			DataFlowAnalysis? dataflow = model.AnalyzeDataFlow( methodBody );
			if( dataflow == null ) {
				return;
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
			INamedTypeSymbol? type
		) {
			if( type == null ) {
				return false;
			}

			if( type.Equals( readOnlyAttribute, SymbolEqualityComparer.Default ) ) {
				return true;
			}

			return IsReadOnlyAttribute( readOnlyAttribute, type.BaseType );
		}

		private static bool HasOnlyAttributeCandidates( ParameterSyntax parameter ) {

			SyntaxList<AttributeListSyntax> attributeLists = parameter.AttributeLists;
			if( attributeLists.Count == 0 ) {
				return false;
			}

			foreach( AttributeListSyntax attributeList in attributeLists ) {
				foreach( AttributeSyntax attribute in attributeList.Attributes ) {

					string unqualifiedName = attribute.Name.GetUnqualifiedNameAsString();
					if( UnqualifiedAttributeNames.Contains( unqualifiedName ) ) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
