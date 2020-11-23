using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutableAttributeConsistencyAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ImmutableAnnotationMismatch,
			Diagnostics.MissingTransitiveImmutableAttribute
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private static void CompilationStart(
			CompilationStartAnalysisContext context
		) {
			ImmutabilityContext immutabilityContext = ImmutabilityContext.Create( context.Compilation );

			context.RegisterSymbolAction(
				ctx => AnalyzeTypeDeclaration(
					ctx,
					immutabilityContext,
					(INamedTypeSymbol)ctx.Symbol
				),
				SymbolKind.NamedType
			);
		}

		private static void AnalyzeTypeDeclaration(
			SymbolAnalysisContext ctx,
			ImmutabilityContext immutabilityContext,
			INamedTypeSymbol analyzedType
		) {
			ImmutableTypeInfo analyzedTypeInfo = immutabilityContext.GetImmutableTypeInfo( analyzedType );

			var typesToConsider = GatherTypesToConsider( analyzedType );
			foreach( INamedTypeSymbol consideredType in typesToConsider ) {
				if( consideredType.MetadataName == "IEnumerable`1" ) continue;

				ImmutableTypeInfo consideredTypeInfo = immutabilityContext.GetImmutableTypeInfo( consideredType );

				if( consideredTypeInfo.Kind == ImmutableTypeKind.None ) {
					// This class / interface isn't marked Immutable in any form
					continue;
				}

				if( consideredTypeInfo.Kind.HasFlag( ImmutableTypeKind.Total )
					// Don't require class Foo : IEnumerable<object> to be [Immutable]
					&& consideredTypeInfo.IsImmutableDefinition(
						immutabilityContext,
						consideredType,
						() => null,
						out _
					)
				) {
					// We should be marked [Immutable]
					if( !analyzedTypeInfo.Kind.HasFlag( ImmutableTypeKind.Total ) ) {
						ctx.ReportDiagnostic( Diagnostic.Create(
							Diagnostics.MissingTransitiveImmutableAttribute,
							location: ( analyzedType.DeclaringSyntaxReferences.First().GetSyntax() as TypeDeclarationSyntax ).Identifier.GetLocation(),
							analyzedType.MetadataName,
							consideredType.TypeKind,
							consideredType.MetadataName
						) );
					}
				}

				int i = 0;
				var paramArgPairs = consideredType.TypeParameters.Zip( consideredType.TypeArguments, ( p, a ) => (p, a, i++) );
				foreach( var (parameter, argument, position) in paramArgPairs ) {
					// We could enforce that arguments and parameters be exactly the same.
					// However that may be too restrictive, and what's more important is that
					// an implemented type isn't more lax than us
					// e.g.
					// [Immutable] interface IFoo<T> {}
					// [Immutable] class Foo<[Immutable] T> : IFoo<T> {}
					// Foo is only considered immutable if T is immutable as well; however,
					// IFoo is considered immutable no matter what T is. This is bad.
					// The other direction doesn't matter, because it would mean IFoo is more
					// strict than Foo, and thus still safe.
					if( argument is ITypeParameterSymbol && Attributes.Objects.Immutable.IsDefined( argument ) ) {
						if( !consideredTypeInfo.IsImmutableTypeParameter( parameter ) ) {
							ctx.ReportDiagnostic( Diagnostic.Create(
								Diagnostics.ImmutableAnnotationMismatch,
								// TODO: this location is garbage, just needed to put it somewhere
								location: ( analyzedType.DeclaringSyntaxReferences.First().GetSyntax() as TypeDeclarationSyntax ).Identifier.GetLocation()
							) );
						}
					}
				}
			}
		}

		private static ImmutableArray<INamedTypeSymbol> GatherTypesToConsider(
			INamedTypeSymbol analyzedType
		) {
			var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

			if( analyzedType.BaseType != null ) {
				builder.Add( analyzedType.BaseType );
			}

			builder.AddRange( analyzedType.Interfaces );

			return builder.ToImmutable();
		}
	}
}
