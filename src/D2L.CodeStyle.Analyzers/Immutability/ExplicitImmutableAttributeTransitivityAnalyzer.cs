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

			// So we don't have [Immutable]: check if we should for some reason

			foreach( var iface in type.Interfaces ) {
				if( hasTheImmutableAttribute( iface ) ) {
					context.ReportDiagnostic(
						CreateDiagnostic(
							declaredThing: type,
							declSyntax: decl,
							otherThing: iface,
							otherThingKind: "interface"
						)
					);
					return;
				}
			}

			// Only thing left to check is the base type

			if( type.BaseType == null ) {
				return;
			}

			if ( hasTheImmutableAttribute( type.BaseType ) ) {
				context.ReportDiagnostic(
					CreateDiagnostic(
						declaredThing: type,
						declSyntax: decl,
						otherThing: type.BaseType,
						otherThingKind: "base class"
					)
				);
				return;
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
