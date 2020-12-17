﻿using System.Collections.Immutable;
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
			Diagnostics.InvalidAuditType
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public static void CompilationStart(
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

			context.RegisterSymbolAction(
				ctx => AnalyzeMember( ctx, immutabilityContext ),
				SymbolKind.Field,
				SymbolKind.Property
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeTypeArguments(
					ctx,
					immutabilityContext,
					(SimpleNameSyntax)ctx.Node
				),
				SyntaxKind.IdentifierName,
				SyntaxKind.GenericName
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeConditionalImmutabilityOnMethodDeclarations,
				SyntaxKind.MethodDeclaration,
				SyntaxKind.LocalFunctionStatement
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeConflictingImmutabilityOnTypeParameters,
				SyntaxKind.TypeParameter
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeConflictingImmutabilityOnMember,
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.StructDeclaration
			);
		}

		private static void AnalyzeMember(
			SymbolAnalysisContext ctx,
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
				context: immutabilityContext
			);

			checker.CheckMember( ctx.Symbol );
		}

		private static void AnalyzeTypeDeclaration(
			SymbolAnalysisContext ctx,
			ImmutabilityContext immutabilityContext,
			INamedTypeSymbol typeSymbol
		) {
			if( typeSymbol.IsImplicitlyDeclared ) {
				return;
			}

			if( typeSymbol.TypeKind == TypeKind.Interface ) {
				return;
			}

			if( !Attributes.Objects.Immutable.IsDefined( typeSymbol )
				&& !Attributes.Objects.ConditionallyImmutable.IsDefined( typeSymbol )
				&& !Attributes.Objects.ImmutableBaseClass.IsDefined( typeSymbol )
			) {
				return;
			}

			if( Attributes.Objects.ConditionallyImmutable.IsDefined( typeSymbol ) ) {
				immutabilityContext = immutabilityContext.WithConditionalTypeParametersAsImmutable( typeSymbol );
			}

			ImmutableDefinitionChecker checker = new ImmutableDefinitionChecker(
				compilation: ctx.Compilation,
				diagnosticSink: ctx.ReportDiagnostic,
				context: immutabilityContext
			);

			checker.CheckDeclaration( typeSymbol );
		}

		private static void AnalyzeTypeArguments(
			SyntaxNodeAnalysisContext ctx,
			ImmutabilityContext immutabilityContext,
			SimpleNameSyntax syntax
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
				if( !Attributes.Objects.Immutable.IsDefined( parameter ) ) {
					continue;
				}

				if( !immutabilityContext.IsImmutable(
					type: argument,
					kind: ImmutableTypeKind.Total,
					// If the syntax is a GenericName (has explicit type arguments) then the error should be on the argument
					// Otherwise, it should be on the identifier itself
					getLocation: () => syntax is GenericNameSyntax genericSyntax
						? genericSyntax.TypeArgumentList.Arguments[position].GetLocation()
						: syntax.Identifier.GetLocation(),
					out Diagnostic diagnostic
				) ) {
					// TODO: not necessarily a good diagnostic for this use-case
					ctx.ReportDiagnostic( diagnostic );
				}
			}
		}

		private static void AnalyzeConditionalImmutabilityOnMethodDeclarations( SyntaxNodeAnalysisContext ctx ) {
			// Get the symbol for the method
			if( ctx.SemanticModel.GetDeclaredSymbol( ctx.Node ) is not IMethodSymbol symbol ) {
				return;
			}

			foreach( var parameter in symbol.TypeParameters ) {
				// Check if the parameter has the [OnlyIf] attribute
				if( !Attributes.Objects.OnlyIf.IsDefined( parameter ) ) {
					continue;
				}

				// Create the diagnostic on the parameter (including the attribute)
				var diagnostic = Diagnostic.Create(
					Diagnostics.UnexpectedConditionalImmutability,
					parameter.DeclaringSyntaxReferences[0].GetSyntax().GetLocation() );
				ctx.ReportDiagnostic( diagnostic );
			}
		}

		private static void AnalyzeConflictingImmutabilityOnTypeParameters( SyntaxNodeAnalysisContext ctx ) {
			// Get the symbol for the parameter
			if( ctx.SemanticModel.GetDeclaredSymbol( ctx.Node ) is not ITypeParameterSymbol symbol ) {
				return;
			}

			// Check if the parameter has both the [Immutable] and the [OnlyIf] attributes
			if( !Attributes.Objects.Immutable.IsDefined( symbol ) || !Attributes.Objects.OnlyIf.IsDefined( symbol ) ) {
				return;
			}

			// Create the diagnostic on the parameter (excluding the attribute)
			var diagnostic = Diagnostic.Create(
				Diagnostics.ConflictingImmutability,
				symbol.DeclaringSyntaxReferences[0].GetSyntax().GetLastToken().GetLocation(),
				"Immutable",
				"ConditionallyImmutable.OnlyIf",
				symbol.Kind.ToString().ToLower() );
			ctx.ReportDiagnostic( diagnostic );
		}

		private static void AnalyzeConflictingImmutabilityOnMember(
			SyntaxNodeAnalysisContext ctx ) {

			// Ensure syntax is expected and get the symbol
			if( ctx.Node is not TypeDeclarationSyntax syntax ) {
				return;
			}
			var symbol = ctx.SemanticModel.GetDeclaredSymbol( syntax );

			// Get information about immutability
			bool hasImmutable = Attributes.Objects.Immutable.IsDefined( symbol );
			bool hasConditionallyImmutable = Attributes.Objects.ConditionallyImmutable.IsDefined( symbol );
			bool hasImmutableBase = Attributes.Objects.ImmutableBaseClass.IsDefined( symbol );

			// Check if there are conflicting immutability attributes
			if( hasImmutable && hasConditionallyImmutable ) {
				// [Immutable] and [ConditionallyImmutable] both exist,
				// so create a diagnostic
				var diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					"Immutable",
					"ConditionallyImmutable",
					syntax.Keyword );
				ctx.ReportDiagnostic( diagnostic );
			}
			if( hasImmutable && hasImmutableBase ) {
				// [Immutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				var diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					"Immutable",
					"ImmutableBaseClassAttribute",
					syntax.Keyword );
				ctx.ReportDiagnostic( diagnostic );
			}
			if( hasConditionallyImmutable && hasImmutableBase ) {
				// [ConditionallyImmutable] and [ImmutableBaseClassAttribute] both exist,
				// so create a diagnostic
				var diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingImmutability,
					syntax.Identifier.GetLocation(),
					"ConditionallyImmutable",
					"ImmutableBaseClassAttribute",
					syntax.Keyword );
				ctx.ReportDiagnostic( diagnostic );
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
