using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class TypeDeclarationImmutabilityAnalyzer : DiagnosticAnalyzer {

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
			Diagnostics.UnnecessaryMutabilityAnnotation
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public static void CompilationStart(
			CompilationStartAnalysisContext context
		) {
			ImmutabilityContext immutabilityContext = ImmutabilityContext.Create( context.Compilation );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeTypeDeclaration(
					ctx,
					immutabilityContext,
					(TypeDeclarationSyntax)ctx.Node
				),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.StructDeclaration
			);
		}

		private static void AnalyzeTypeDeclaration(
			SyntaxNodeAnalysisContext ctx,
			ImmutabilityContext immutabilityContext,
			TypeDeclarationSyntax typeDeclaration
		) {
			SemanticModel model = ctx.Compilation.GetSemanticModel( typeDeclaration.SyntaxTree );

			INamedTypeSymbol typeSymbol = model.GetDeclaredSymbol( typeDeclaration, ctx.CancellationToken );
			if( !ShouldAnalyze( typeSymbol ) ) {
				return;
			}

			ImmutableDefinitionChecker checker = new ImmutableDefinitionChecker(
				model: model,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext
			);

			switch( typeDeclaration ) {
				case ClassDeclarationSyntax classDeclaration:
					checker.CheckClass( classDeclaration );
					break;
				case StructDeclarationSyntax structDeclaration:
					checker.CheckStruct( structDeclaration );
					break;
			}
		}

		private static bool ShouldAnalyze(
			INamedTypeSymbol analyzedType
		) {
			if( Attributes.Objects.Immutable.IsDefined( analyzedType ) ) {
				return true;
			}

			if( Attributes.Objects.ImmutableBaseClass.IsDefined( analyzedType ) ) {
				return true;
			}

			return false;
		}
	}
}
