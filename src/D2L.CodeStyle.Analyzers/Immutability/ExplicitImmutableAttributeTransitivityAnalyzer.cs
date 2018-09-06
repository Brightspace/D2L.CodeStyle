using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	// [Immutable] is a pledge (with checking) that values of your type *or any
	// subtype* are immutable. In other words the effects/checks for
	// [Immutable] apply transitively to your subtypes. Rather than this
	// being implicit, we make it explicit by requiring your subtypes to be
	// annotated with [Immutable]. This is useful documentation and keeps the
	// implementation for the mutability analysis simple.

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ExplicitImmutableAttributeTransitivityAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.MissingTransitiveImmutableAttribute
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public void CompilationStart(
			CompilationStartAnalysisContext context
		) {
			var immutableAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.Objects+Immutable"
			);

			// If we implement an interface with [Immutable] or derive a base
			// class with it this symbol would have been found... if it wasn't
			// we don't need to analyze anything.
			if ( immutableAttribute == null ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeTypeDeclaration( ctx, hasTheImmutableAttribute ),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.StructDeclaration,
				SyntaxKind.InterfaceDeclaration
			);

			// helper methods:

			bool hasTheImmutableAttribute( ITypeSymbol type ) {
				return type.GetAttributes().Any( isTheImmutableAttribute );
			}

			bool isTheImmutableAttribute( AttributeData attr ) {
				return attr.AttributeClass == immutableAttribute;
			}
		}

		public void AnalyzeTypeDeclaration(
			SyntaxNodeAnalysisContext context,
			Func<ITypeSymbol, bool> hasTheImmutableAttribute
		) {
			var decl = (TypeDeclarationSyntax)context.Node;
			var type = context.SemanticModel.GetDeclaredSymbol( decl );

			var typeHasImmutableAttr = hasTheImmutableAttribute( type );

			if( typeHasImmutableAttr ) {
				// Nothing to do: we already have the [Immutable] attribute.
				return;
			}

			// The BaseList is the syntax for the ": A, B, C" part which may
			// include a base class and any number of interfaces. We aren't
			// looking at type.Interfaces and type.BaseType (from the semantic
			// model) because we don't want to produce multiple diagnostics for
			// partial classes. Instead, we attach any diagnostic to whatever
			// partial declaration mentions a base class or interface that is
			// [Immutable].

			if ( decl.BaseList == null ) {
				// No base class or interfaces on this decl so nothing to check.
				return;
			}

			foreach( var baseType in decl.BaseList.Types ) {
				var typeSymbol = context.SemanticModel
					.GetSymbolInfo( baseType.Type ).Symbol
					// Not aware of any reason this cast could fail:
					as ITypeSymbol;

				if ( !hasTheImmutableAttribute( typeSymbol ) ) {
					continue;
				}

				// The docs say the only things that can have BaseType == null
				// are interfaces, System.Object itself (won't come up in our
				// analysis because (1) it !hasTheImmutableAttribute (2) you
				// can't explicitly list it as a base class anyway) and
				// pointer types (the base value type probably also doesn't
				// have it.)
				bool isInterface =
					typeSymbol.BaseType == null
					&& typeSymbol.SpecialType != SpecialType.System_Object
					&& typeSymbol.SpecialType != SpecialType.System_ValueType
					&& typeSymbol.Kind != SymbolKind.PointerType;

				context.ReportDiagnostic(
					CreateDiagnostic(
						declaredThing: type,
						declSyntax: decl,
						otherThing: typeSymbol,
						otherThingKind: isInterface ? "interface" : "base class"
					)
				);
			}
		}

		public Diagnostic CreateDiagnostic(
			ITypeSymbol declaredThing,
			TypeDeclarationSyntax declSyntax,
			ITypeSymbol otherThing,
			string otherThingKind
		) {
			return Diagnostic.Create(
				Diagnostics.MissingTransitiveImmutableAttribute,
				declSyntax.Identifier.GetLocation(),
				declaredThing.GetFullTypeName(),
				otherThingKind,
				otherThing.GetFullTypeName()
			);
		}
	}
}
