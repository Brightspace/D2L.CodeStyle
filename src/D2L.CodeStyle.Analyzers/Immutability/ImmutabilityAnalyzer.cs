#nullable disable

using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed partial class ImmutabilityAnalyzer : DiagnosticAnalyzer {

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
			Diagnostics.InconsistentMethodAttributeApplication,
			Diagnostics.UnappliedConditionalImmutability
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

			ImmutableTypeParameterArgumentAnalysis.Register(
				context,
				annotationsContext,
				immutabilityContext
			);

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

			context.RegisterSymbolAction(
				ctx => AnalyzeConflictingImmutabilityOnTypeParameters(
					ctx,
					(INamedTypeSymbol)ctx.Symbol,
					annotationsContext
				),
				SymbolKind.NamedType
			);

			context.RegisterSymbolAction(
				ctx => AnalyzeConflictingImmutabilityOnMember(
					ctx,
					(INamedTypeSymbol)ctx.Symbol,
					annotationsContext
				),
				SymbolKind.NamedType
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
			SymbolAnalysisContext ctx,
			INamedTypeSymbol namedTypeSymbol,
			AnnotationsContext annotationsContext
		) {
			foreach( var parameter in namedTypeSymbol.TypeParameters ) {
				// Check if the parameter has both the [Immutable] and the [OnlyIf] attributes
				if( !annotationsContext.Objects.Immutable.IsDefined( parameter )
					|| !annotationsContext.Objects.OnlyIf.IsDefined( parameter )
				) {
					return;
				}

				// Create the diagnostic on the parameter (excluding the attribute)
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					parameter.Locations[0],
					messageArgs: new[] {
						"Immutable",
						"ConditionallyImmutable.OnlyIf",
						"typeparameter"
					}
				);
			}
		}

		private static void AnalyzeConflictingImmutabilityOnMember(
			SymbolAnalysisContext ctx,
			INamedTypeSymbol symbol,
			AnnotationsContext annotationsContext
		) {
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
					symbol.Locations[ 0 ],
					messageArgs: new object[] {
						"Immutable",
						"ConditionallyImmutable",
						KindName( symbol )
					}
				);
			}
			if( hasImmutable && hasImmutableBase ) {
				// [Immutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					symbol.Locations[ 0 ],
					messageArgs: new object[] {
						"Immutable",
						"ImmutableBaseClassAttribute",
						KindName( symbol )
					}
				);
			}
			if( hasConditionallyImmutable && hasImmutableBase ) {
				// [ConditionallyImmutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				ctx.ReportDiagnostic(
					Diagnostics.ConflictingImmutability,
					symbol.Locations[ 0 ],
					messageArgs: new object[] {
						"ConditionallyImmutable",
						"ImmutableBaseClassAttribute",
						KindName( symbol )
					}
				 );
			}

			static string KindName( INamedTypeSymbol symbol ) => symbol.TypeKind switch {
				TypeKind.Class => "class",
				TypeKind.Interface => "interface",
				TypeKind.Struct => "struct",
				_ => symbol.TypeKind.ToString()
			};
		}
	}
}
