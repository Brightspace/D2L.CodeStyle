using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ImmutableGenericAttributeAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.ImmutableGenericAttributeInWrongAssembly,
				Diagnostics.ImmutableGenericAttributeAppliedToNonGenericType,
				Diagnostics.ImmutableGenericAttributeAppliedToOpenGenericType
			);

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationAction( AnalyzeAssembly );
		}

		private static void AnalyzeAssembly( CompilationAnalysisContext ctx ) {
			var assembly = ctx.Compilation.Assembly;
			var attributes = Attributes.Objects.ImmutableGeneric.GetAll( assembly );
			foreach( var attr in attributes ) {
				AnalyzeAttribute( ctx, assembly, attr );
			}
		}

		private static void AnalyzeAttribute( 
			CompilationAnalysisContext ctx, 
			IAssemblySymbol currentAssembly, 
			AttributeData attr 
		) {
			if( attr.ConstructorArguments.Length != 1) {
				// compile error
				return;
			}

			var typeBeingMarkedImmutable = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
			if( typeBeingMarkedImmutable == null ) {
				// compile error
				return;
			}

			// check if the type is generic
			if( !typeBeingMarkedImmutable.IsGenericType ) {
				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.ImmutableGenericAttributeAppliedToNonGenericType,
					attr.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
					typeBeingMarkedImmutable.GetFullTypeNameWithGenericArguments()
				) );
				return;
			}

			// check if the type is an open generic, i.e., ISomething<>
			var isUnboundGeneric = typeBeingMarkedImmutable.IsUnboundGenericType;
			var isUnconstructedGeneric = typeBeingMarkedImmutable.TypeArguments.Any( ta => ta.TypeKind == TypeKind.TypeParameter );
			if( isUnboundGeneric || isUnconstructedGeneric ) {
				ctx.ReportDiagnostic( 
					Diagnostic.Create(
						Diagnostics.ImmutableGenericAttributeAppliedToOpenGenericType,
						attr.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
						typeBeingMarkedImmutable.GetFullTypeNameWithGenericArguments()
					)
				);
				return;
			}

			// check if the type is defined in the current assembly
			if( typeBeingMarkedImmutable.ContainingAssembly.Equals( currentAssembly ) ) {
				return;
			}

			// otherwise, check if any of the type arguments are in the current assembly
			foreach( var typeArgument in typeBeingMarkedImmutable.TypeArguments ) {
				if( typeArgument.ContainingAssembly.Equals( currentAssembly ) ) {
					return;
				}
			}

			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.ImmutableGenericAttributeInWrongAssembly,
					attr.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
					typeBeingMarkedImmutable.GetFullTypeNameWithGenericArguments()
				)
			);
		}
	}
}
