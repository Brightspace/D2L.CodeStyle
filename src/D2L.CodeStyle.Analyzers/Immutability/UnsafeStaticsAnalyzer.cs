using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class UnsafeStaticsAnalyzer : DiagnosticAnalyzer {
		public const string PROPERTY_FIELDORPROPNAME = "FieldOrProprName";
		public const string PROPERTY_OFFENDINGTYPE = "OffendingType";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.UnsafeStatic,
			Diagnostics.ConflictingStaticAnnotation,
			Diagnostics.UnnecessaryStaticAnnotation
		);

		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			// We aren't analyzing code-gen'd files. This is not ideal
			// long-term because it may cause us to miss things but is
			// being done temporarily because of the large signal-to-noise
			// ratio.
			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.None
			);

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			var inspector = new MutabilityInspector( context.Compilation );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeField( ctx, inspector ),
				SyntaxKind.FieldDeclaration
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeProperty( ctx, inspector ),
				SyntaxKind.PropertyDeclaration
			);
		}

		private void AnalyzeField(
			SyntaxNodeAnalysisContext context,
			MutabilityInspector inspector
		) {
			var root = context.Node as FieldDeclarationSyntax;

			if( root == null ) {
				throw new Exception( "This should not happen if this function is wired up correctly" );
			}

			bool isStatic = root.Modifiers.Any( SyntaxKind.StaticKeyword );

			foreach( var variable in root.Declaration.Variables ) {
				var symbol = context
					.SemanticModel
					.GetDeclaredSymbol( variable )
					as IFieldSymbol;

				if( symbol == null ) {
					// Could this happen? We are not emitting diagnostics in this
					// case even when the fields have an annotation.
					continue;
				}

				InspectFieldOrProperty(
					context,
					inspector,
					fieldOrProperty: symbol,
					location: variable.GetLocation(),
					isStatic: isStatic,
					fieldOrPropertyType: symbol.Type,
					fieldOrPropertyName: variable.Identifier.ValueText
				);
			}
		}

		private void AnalyzeProperty(
			SyntaxNodeAnalysisContext context,
			MutabilityInspector inspector
		) {
			var root = context.Node as PropertyDeclarationSyntax;

			if( root == null ) {
				throw new Exception( "This should not happen if this function is wired up correctly" );
			}

			bool isStatic = root.Modifiers.Any( SyntaxKind.StaticKeyword );

			var prop = context.SemanticModel.GetDeclaredSymbol( root );

			if( prop == null ) {
				// Could this happen? We are not emitting diagnostics in this case
				// even when the property has an annotation.
				return;
			}

			InspectFieldOrProperty(
				context,
				inspector,
				fieldOrProperty: prop,
				location: root.GetLocation(),
				isStatic: isStatic,
				fieldOrPropertyType: prop.Type,
				fieldOrPropertyName: prop.Name
			);
		}

		/// <summary>
		/// This helper method implements all of the shared logic. We have to
		/// split this for two reasons:
		///
		/// 1. PropertyDeclarationSyntax and FieldDeclarationSyntaxs useful
		///    members don't come from a base class/interface so we can't do
		///    this generically.
		/// 2. Fields can define multiple variables and we may wish to output
		///    multiple diagnostics (individual variables in a field declaration
		///    may be alright because of their initializers.)
		/// </summary>
		private void InspectFieldOrProperty(
			SyntaxNodeAnalysisContext context,
			MutabilityInspector inspector,
			ISymbol fieldOrProperty,
			Location location,
			bool isStatic,
			ITypeSymbol fieldOrPropertyType,
			string fieldOrPropertyName
		) {
			bool hasUnauditedAnnotation = Attributes.Statics.Unaudited
				.IsDefined( fieldOrProperty );

			bool hasAuditedAnnotation = Attributes.Statics.Audited
				.IsDefined( fieldOrProperty );

			bool hasAnnotation = hasUnauditedAnnotation || hasAuditedAnnotation;

			if ( !isStatic && hasAnnotation ) {
				// non-statics don't need these annotations.
				// TODO: Consider dropping the static-specific annotations in
				// favour of Mutability.Audited/Unaudited. Will require some
				// effort due to the reporting we do but it'll make things
				// simpler once we merge the analyzers.
				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.UnnecessaryStaticAnnotation,
						location,
						ImmutableDictionary<string, string>.Empty,
						hasAuditedAnnotation ? "Statics.Audited" : "Statics.Unaudited",
						fieldOrPropertyName
					)
				);

				return;
			}

			if( !isStatic ) {
				return;
			}

			if( hasAuditedAnnotation && hasUnauditedAnnotation ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.ConflictingStaticAnnotation,
						location
					)
				);

				// Bail out here because it's unclear which of the remaining
				// diagnostics for this field/property should apply.
				return;
			}

			var result = inspector.InspectMember( fieldOrProperty );

			Diagnostic diagnostic = null;
			if ( result.IsMutable ) {
				diagnostic = CreateDiagnostic( 
					location, 
					fieldOrPropertyName,
					fieldOrPropertyType.GetFullTypeNameWithGenericArguments(), 
					result 
				);
			}

			var hasDiagnostic = diagnostic != null;

			// Unnecessary annotations clutter the code base. Because
			// Statics.Audited and Statics.Unaudited enable things to build
			// that shouldn't otherwise we would like to keep the list of
			// annotations small and this covers the easy case. This also
			// provides assurance that we don't start marking things as safe
			// that we previously wouldn't due to an analyzer change (the
			// existing Statics.Audited and Statics.Unaudited serve as test
			// cases in a way.)
			if( hasAnnotation && !hasDiagnostic ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.UnnecessaryStaticAnnotation,
						location,
						ImmutableDictionary<string, string>.Empty,
						hasAuditedAnnotation ? "Statics.Audited" : "Statics.Unaudited",
						fieldOrPropertyName
					)
				);

				// this bail-out isn't important because !hasDiagnostic
				return;
			}

			if ( hasAnnotation ) {
				// the annotations supress remaining diagnostics
				return;
			}

			if ( hasDiagnostic ) {
				context.ReportDiagnostic( diagnostic );
			}
		}

		private Diagnostic CreateDiagnostic( 
			Location location, 
			string fieldOrPropName, 
			string offendingType, 
			MutabilityInspectionResult result 
		) {
			var builder = ImmutableDictionary.CreateBuilder<string, string>();
			builder[PROPERTY_FIELDORPROPNAME] = fieldOrPropName;
			builder[PROPERTY_OFFENDINGTYPE] = offendingType;
			var properties = builder.ToImmutable();

			var reason = m_resultFormatter.Format( result );

			var diagnostic = Diagnostic.Create(
				Diagnostics.UnsafeStatic,
				location,
				properties,
				fieldOrPropName,
				reason
			);
			return diagnostic;
		}
	}
}
