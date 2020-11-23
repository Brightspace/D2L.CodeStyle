using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutableAttributeConsistencyAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ImmutableTypeParameterAppliedToNonImmutableParameter,
			Diagnostics.MissingTransitiveImmutableAttribute,
			Diagnostics.UnusedImmutableTypeParameter
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
			ImmutableArray<ITypeParameterSymbol> analyzedTypeImmutableTypeParameters = analyzedType
				.TypeParameters
				.Where( p => analyzedTypeInfo.IsImmutableTypeParameter( p ) )
				.ToImmutableArray();
			
			var typesToConsider = GatherTypesToConsider( analyzedType );
			foreach( INamedTypeSymbol consideredType in typesToConsider ) {
				if( consideredType.MetadataName == "IEnumerable`1" ) continue;

				ImmutableTypeInfo consideredTypeInfo = immutabilityContext.GetImmutableTypeInfo( consideredType );

				if( consideredTypeInfo.Kind == ImmutableTypeKind.None ) {
					// This class / interface being implemented isn't marked Immutable in any form
					continue;
				}

				// Ensure that for Foo<[Immutable] T, U, [Immutable] V>, T and V are used to implement all [Immutable(ish)] types
				foreach( ITypeParameterSymbol parameter in analyzedTypeImmutableTypeParameters ) {
					if( consideredType.TypeArguments.Any( arg => parameter.Equals( arg ) ) ) {
						continue;
					}

					ctx.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.UnusedImmutableTypeParameter,
						GetLocationOfInheritence( ctx.Compilation, analyzedType, consideredType ),
						parameter.Name, analyzedType.TypeKind, analyzedType.MetadataName
					) );
				}


				bool consideredTypeImmutabilityVariesOnTypeParameters = false;

				int i = 0;
				var paramArgPairs = consideredType.TypeParameters.Zip( consideredType.TypeArguments, ( p, a ) => (p, a, i++) );
				foreach( var (parameter, argument, position) in paramArgPairs ) {
					if( !( argument is ITypeParameterSymbol argumentAsTypeParameter ) ) {
						continue;
					}

					bool isImmutableTypeParameter = consideredTypeInfo.IsImmutableTypeParameter( parameter );

					consideredTypeImmutabilityVariesOnTypeParameters =
						consideredTypeImmutabilityVariesOnTypeParameters || isImmutableTypeParameter;

					if( !analyzedTypeInfo.IsImmutableTypeParameter( argumentAsTypeParameter ) ) {
						continue;
					}

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
					if( !isImmutableTypeParameter ) {
						ctx.ReportDiagnostic( Diagnostic.Create(
							Diagnostics.ImmutableTypeParameterAppliedToNonImmutableParameter,
							// TODO: this location isn't amazing, just needed to put it somewhere.
							// Also, maybe Lazy<> this and re-use it
							GetLocationOfInheritence( ctx.Compilation, analyzedType, consideredType ),
							argument.Name, analyzedType.TypeKind, analyzedType.MetadataName,
							parameter.Name, consideredType.TypeKind, consideredType.MetadataName
						) );
					}
				}

				if( consideredTypeImmutabilityVariesOnTypeParameters
					|| consideredTypeInfo.IsImmutableDefinition(
						immutabilityContext,
						consideredType,
						() => null,
						out _
					)
				) {
					ctx.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.MissingTransitiveImmutableAttribute,
						( analyzedType.DeclaringSyntaxReferences.First().GetSyntax() as TypeDeclarationSyntax ).Identifier.GetLocation(),
						analyzedType.MetadataName,
						consideredType.TypeKind, consideredType.MetadataName
					) );
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

		private static Location GetLocationOfInheritence(
			Compilation compilation,
			INamedTypeSymbol type,
			INamedTypeSymbol inheritedType
		) {
			var candidates = type.DeclaringSyntaxReferences
				.Select( r => r.GetSyntax() )
				.Cast<ClassDeclarationSyntax>()
				.Where( r => r.BaseList != null )
				.SelectMany( r => r.BaseList.Types );

			// Find the first candidate that is a class type.
			foreach( var candidate in candidates ) {
				var model = compilation.GetSemanticModel( candidate.SyntaxTree );
				var candidateInfo = model.GetTypeInfo( candidate.Type );

				if( candidateInfo.Type == null ) {
					continue;
				}

				if( candidateInfo.Type.Equals( inheritedType ) ) {
					return candidate.GetLocation();
				}
			}

			// If we couldn't find a candidate just use the first class decl
			// as the diagnostic target. I'm not sure this can happen.
			return ( type.DeclaringSyntaxReferences.First().GetSyntax() as TypeDeclarationSyntax )
				.Identifier
				.GetLocation();
		}
	}
}
