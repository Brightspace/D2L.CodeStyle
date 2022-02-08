#nullable disable

using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ArraysAreMutable,
			Diagnostics.DelegateTypesPossiblyMutable,
			Diagnostics.DynamicObjectsAreMutable,
			Diagnostics.EventMemberMutable,
			Diagnostics.MemberIsNotReadOnly,
			Diagnostics.NonImmutableTypeHeldByImmutable,
			Diagnostics.TypeParameterIsNotKnownToBeImmutable,
			Diagnostics.UnexpectedMemberKind,
			Diagnostics.UnexpectedTypeKind,
			Diagnostics.UnnecessaryMutabilityAnnotation,
			Diagnostics.UnexpectedConditionalImmutability,
			Diagnostics.ConflictingImmutability,
			Diagnostics.InvalidAuditType,
			Diagnostics.AnonymousFunctionsMayCaptureMutability,
			Diagnostics.UnknownImmutabilityAssignmentKind,

			Diagnostics.MissingTransitiveImmutableAttribute,
			Diagnostics.InconsistentMethodAttributeApplication
		);

		private readonly ImmutableHashSet<string> m_additionalImmutableTypes;

		public ImmutabilityAnalyzer() : this( ImmutableHashSet<string>.Empty ) { }

		public ImmutabilityAnalyzer( ImmutableHashSet<string> additionalImmutableTypes ) {
			m_additionalImmutableTypes = additionalImmutableTypes;
		}

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public void CompilationStart(
			CompilationStartAnalysisContext context
		) {
			if( !AnnotationsContext.TryCreate( context.Compilation, out AnnotationsContext annotationsContext ) ) {
				return;
			}
			ImmutabilityContext immutabilityContext = ImmutabilityContext.Create( context.Compilation, annotationsContext, m_additionalImmutableTypes );

			context.RegisterSymbolAction(
				ctx => AnalyzeTypeDeclaration(
					ctx,
					annotationsContext,
					immutabilityContext,
					(INamedTypeSymbol)ctx.Symbol
				),
				SymbolKind.NamedType
			);

			context.RegisterSymbolAction(
				ctx => AnalyzeMethodDeclarationConsistency(
					ctx,
					annotationsContext,
					immutabilityContext,
					(IMethodSymbol)ctx.Symbol
				),
				SymbolKind.Method
			);

			context.RegisterSymbolAction(
				ctx => AnalyzeMember( ctx, annotationsContext, immutabilityContext ),
				SymbolKind.Field,
				SymbolKind.Property
			);

			context.RegisterSyntaxNodeAction(
				ctx => {
					IdentifierNameSyntax identifierName = (IdentifierNameSyntax)ctx.Node;
					AnalyzeTypeArguments(
						ctx,
						annotationsContext,
						immutabilityContext,
						identifierName,
						getArgumentLocation: _ => identifierName.Identifier.GetLocation()
					);
				},
				SyntaxKind.IdentifierName
			);

			context.RegisterSyntaxNodeAction(
				ctx => {
					GenericNameSyntax genericName = (GenericNameSyntax)ctx.Node;
					AnalyzeTypeArguments(
						ctx,
						annotationsContext,
						immutabilityContext,
						genericName,
						getArgumentLocation: position => genericName.TypeArgumentList.Arguments[ position ].GetLocation()
					);
				},
				SyntaxKind.GenericName
			);

			context.RegisterSymbolAction(
				ctx => AnalyzeConditionalImmutabilityOnMethodDeclarations(
					(IMethodSymbol)ctx.Symbol,
					annotationsContext,
					ctx.ReportDiagnostic,
					ctx.CancellationToken
				),
				SymbolKind.Method
			);

			context.RegisterSyntaxNodeAction(
				ctx => {
					IMethodSymbol method = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol(
						(LocalFunctionStatementSyntax)ctx.Node,
						ctx.CancellationToken
					);

					AnalyzeConditionalImmutabilityOnMethodDeclarations(
						method,
						annotationsContext,
						ctx.ReportDiagnostic,
						ctx.CancellationToken
					);
				},
				SyntaxKind.LocalFunctionStatement
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeConflictingImmutabilityOnTypeParameters(
					ctx,
					annotationsContext
				),
				SyntaxKind.TypeParameter
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeConflictingImmutabilityOnMember(
					ctx,
					annotationsContext
				),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.StructDeclaration
			);
		}

		private static void AnalyzeMember(
			SymbolAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext
		) {
			// We only care about checking static fields/properties. These
			// are global variables, so we always want them to be immutable.
			// The fields/properties of [Immutable] types get handled via
			// AnalyzeTypeDeclaration.
			if( !ctx.Symbol.IsStatic ) {
				return;
			}

			// Ignore const things, which include enum names.
			if( ctx.Symbol is IFieldSymbol f && f.IsConst ) {
				return;
			}

			// We would like this check to run for generated code too, but
			// there are two problems:
			// (1) the easy one: we generate some static variables that are
			//     safe in practice but don't analyze well.
			// (2) the hard one: resx code-gen generates some stuff that's
			//     safe in practice but doesn't analyze well.
			if( ctx.Symbol.IsFromGeneratedCode() ) {
				return;
			}

			var checker = new ImmutableDefinitionChecker(
				compilation: ctx.Compilation,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext,
				annotationsContext: annotationsContext,
				cancellationToken: ctx.CancellationToken
			);

			checker.CheckMember( ctx.Symbol );
		}

		private static void AnalyzeMethodDeclarationConsistency(
			SymbolAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			IMethodSymbol methodSymbol
		) {
			// Static methods can't implement interface methods
			if( methodSymbol.IsStatic ) {
				return;
			}

			ImmutableAttributeConsistencyChecker consistencyChecker = new ImmutableAttributeConsistencyChecker(
				compilation: ctx.Compilation,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext,
				annotationsContext: annotationsContext
			);

			consistencyChecker.CheckMethodDeclaration( methodSymbol, ctx.CancellationToken );
		}

		private static void AnalyzeTypeDeclaration(
			SymbolAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			INamedTypeSymbol typeSymbol
		) {
			if( typeSymbol.IsImplicitlyDeclared ) {
				return;
			}

			ImmutableAttributeConsistencyChecker consistencyChecker = new ImmutableAttributeConsistencyChecker(
				compilation: ctx.Compilation,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext,
				annotationsContext: annotationsContext
			);

			consistencyChecker.CheckTypeDeclaration( typeSymbol, ctx.CancellationToken );

			if( typeSymbol.TypeKind == TypeKind.Interface ) {
				return;
			}

			if( !annotationsContext.Objects.Immutable.IsDefined( typeSymbol )
				&& !annotationsContext.Objects.ConditionallyImmutable.IsDefined( typeSymbol )
				&& !annotationsContext.Objects.ImmutableBaseClass.IsDefined( typeSymbol )
			) {
				return;
			}

			if( annotationsContext.Objects.ConditionallyImmutable.IsDefined( typeSymbol ) ) {
				immutabilityContext = immutabilityContext.WithConditionalTypeParametersAsImmutable( typeSymbol );
			}

			ImmutableDefinitionChecker checker = new ImmutableDefinitionChecker(
				compilation: ctx.Compilation,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext,
				annotationsContext: annotationsContext,
				cancellationToken: ctx.CancellationToken
			);

			checker.CheckDeclaration( typeSymbol );
		}

		private static void AnalyzeTypeArguments(
			SyntaxNodeAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			SimpleNameSyntax syntax,
			Func<int, Location> getArgumentLocation
		) {
			if( syntax.IsFromDocComment() ) {
				// ignore things in doccomments such as crefs
				return;
			}

			SymbolInfo info = ctx.SemanticModel.GetSymbolInfo( syntax, ctx.CancellationToken );

			// Ignore anything that cannot have type arguments/parameters
			if( !GetTypeParamsAndArgs( info.Symbol, out var typeParameters, out var typeArguments ) ) {
				return;
			}

			int i = 0;
			var paramArgPairs = typeParameters.Zip( typeArguments, ( p, a ) => (p, a, i++) );
			foreach( var (parameter, argument, position) in paramArgPairs ) {
				// TODO: this should eventually use information from ImmutableTypeInfo
				// however the current information about immutable type parameters
				// includes [Immutable] filling for what will instead be the upcoming
				// [OnlyIf] (e.g. it would be broken for IEnumerable<>)
				if( !annotationsContext.Objects.Immutable.IsDefined( parameter ) ) {
					continue;
				}

				if( !immutabilityContext.IsImmutable(
					new ImmutabilityQuery(
						ImmutableTypeKind.Total,
						argument
					),
					getLocation: () => getArgumentLocation( position ),
					out Diagnostic diagnostic
				) ) {
					// TODO: not necessarily a good diagnostic for this use-case
					ctx.ReportDiagnostic( diagnostic );
				}
			}
		}

		private static void AnalyzeConditionalImmutabilityOnMethodDeclarations(
			IMethodSymbol method,
			AnnotationsContext annotationsContext,
			Action<Diagnostic> diagnosticSink,
			CancellationToken cancellationToken
		) {
			foreach( var parameter in method.TypeParameters ) {
				// Check if the parameter has the [OnlyIf] attribute
				if( !annotationsContext.Objects.OnlyIf.IsDefined( parameter ) ) {
					continue;
				}

				// Create the diagnostic on the parameter (including the attribute)
				diagnosticSink( Diagnostic.Create(
					Diagnostics.UnexpectedConditionalImmutability,
					parameter.DeclaringSyntaxReferences[ 0 ].GetSyntax( cancellationToken ).GetLocation()
				) );
			}
		}

		private static void AnalyzeConflictingImmutabilityOnTypeParameters(
			SyntaxNodeAnalysisContext ctx,
			AnnotationsContext annotationsContext
		) {
			// Get the symbol for the parameter
			if( ctx.SemanticModel.GetDeclaredSymbol( ctx.Node, ctx.CancellationToken ) is not ITypeParameterSymbol symbol ) {
				return;
			}

			// Check if the parameter has both the [Immutable] and the [OnlyIf] attributes
			if( !annotationsContext.Objects.Immutable.IsDefined( symbol )
				|| !annotationsContext.Objects.OnlyIf.IsDefined( symbol )
			) {
				return;
			}

			// Create the diagnostic on the parameter (excluding the attribute)
			ctx.ReportDiagnostic(
				Diagnostics.ConflictingImmutability,
				symbol.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken ).GetLastToken().GetLocation(),
				messageArgs: new[] {
					"Immutable",
					"ConditionallyImmutable.OnlyIf",
					symbol.Kind.ToString().ToLower()
				}
			);
		}

		private static void AnalyzeConflictingImmutabilityOnMember(
			SyntaxNodeAnalysisContext ctx,
			AnnotationsContext annotationsContext
		) {

			// Ensure syntax is expected and get the symbol
			if( ctx.Node is not TypeDeclarationSyntax syntax ) {
				return;
			}
			var symbol = ctx.SemanticModel.GetDeclaredSymbol( syntax, ctx.CancellationToken );

			// Get information about immutability
			bool hasImmutable = annotationsContext.Objects.Immutable.IsDefined( symbol );
			bool hasConditionallyImmutable = annotationsContext.Objects.ConditionallyImmutable.IsDefined( symbol );
			bool hasImmutableBase = annotationsContext.Objects.ImmutableBaseClass.IsDefined( symbol );

			// Check if there are conflicting immutability attributes
			if( hasImmutable && hasConditionallyImmutable ) {
				// [Immutable] and [ConditionallyImmutable] both exist,
				// so create a diagnostic
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					messageArgs: new object[] {
						"Immutable",
						"ConditionallyImmutable",
						syntax.Keyword
					}
				);
			}
			if( hasImmutable && hasImmutableBase ) {
				// [Immutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					messageArgs: new object[] {
						"Immutable",
						"ImmutableBaseClassAttribute",
						syntax.Keyword
					}
				);
			}
			if( hasConditionallyImmutable && hasImmutableBase ) {
				// [ConditionallyImmutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					messageArgs: new object[] {
						"ConditionallyImmutable",
						"ImmutableBaseClassAttribute",
						syntax.Keyword
					}
				 );
			}
		}

		private static bool GetTypeParamsAndArgs( ISymbol type, out ImmutableArray<ITypeParameterSymbol> typeParameters, out ImmutableArray<ITypeSymbol> typeArguments ) {
			switch( type ) {
				case IMethodSymbol method:
					typeParameters = method.TypeParameters;
					typeArguments = method.TypeArguments;
					return true;
				case INamedTypeSymbol namedType:
					typeParameters = namedType.TypeParameters;
					typeArguments = namedType.TypeArguments;
					return true;
				default:
					return false;
			}
		}
	}
}
