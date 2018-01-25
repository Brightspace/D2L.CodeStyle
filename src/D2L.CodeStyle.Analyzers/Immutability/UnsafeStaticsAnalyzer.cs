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
			var inspector = new MutabilityInspector(
				context.Compilation,
				new KnownImmutableTypes( context.Compilation.Assembly )
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeField( ctx, inspector ),
				SyntaxKind.FieldDeclaration
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeProperty( ctx, inspector ),
				SyntaxKind.PropertyDeclaration
			);
		}

		private void AnalyzeField( SyntaxNodeAnalysisContext context, MutabilityInspector inspector ) {
			var root = context.Node as FieldDeclarationSyntax;

			if( root == null ) {
				throw new Exception( "This should not happen if this function is wired up correctly" );
			}

			bool isStatic = root.Modifiers.Any( SyntaxKind.StaticKeyword );
			bool isReadOnly = root.Modifiers.Any( SyntaxKind.ReadOnlyKeyword );

			foreach( var variable in root.Declaration.Variables ) {
				var symbol = context.SemanticModel.GetDeclaredSymbol( variable ) as IFieldSymbol;

				if( symbol == null ) {
					// Could this happen? We are not emitting diagnostics in this
					// case even when the fields have an annotation.
					continue;
				}

				InspectFieldOrProperty(
					context,
					inspector,
					location: variable.GetLocation(),
					isStatic: isStatic,
					isReadOnly: isReadOnly,
					fieldOrProperty: symbol,
					fieldOrPropertyType: symbol.Type,
					fieldOrPropertyName: variable.Identifier.ValueText,
					initializer: variable.Initializer?.Value,
					isProperty: false,
					isAutoImplementedProperty: false
				);
			}
		}

		private void AnalyzeProperty( SyntaxNodeAnalysisContext context, MutabilityInspector inspector ) {
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

			bool isAutoImplementedProperty = root.IsAutoImplemented();

			InspectFieldOrProperty(
				context,
				inspector,
				location: root.GetLocation(),
				isStatic: isStatic,
				isReadOnly: prop.IsReadOnly,
				fieldOrProperty: prop,
				fieldOrPropertyType: prop.Type,
				fieldOrPropertyName: prop.Name,
				initializer: root.Initializer?.Value,
				isProperty: true,
				isAutoImplementedProperty: isAutoImplementedProperty
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
		///    multiple diagnostics (individual ariables in a field declaration
		///    may be alright because of their initializers.)
		///
		/// The arguments are organized roughly based on the common grammar of
		/// fields and properties: attributes, modifiers, type, name,
		/// initializer.
		/// </summary>
		private void InspectFieldOrProperty(
			SyntaxNodeAnalysisContext context,
			MutabilityInspector inspector,
			Location location,
			ISymbol fieldOrProperty,
			bool isStatic,
			bool isReadOnly,
			ITypeSymbol fieldOrPropertyType,
			string fieldOrPropertyName,
			ExpressionSyntax initializer,
			bool isProperty,
			bool isAutoImplementedProperty
		) {

			var diagnostics = GatherDiagnostics(
				context.SemanticModel,
				inspector,
				location: location,
				isStatic: isStatic,
				isReadOnly: isReadOnly,
				fieldOrPropertyType: fieldOrPropertyType,
				fieldOrPropertyName: fieldOrPropertyName,
				initializationExpression: initializer,
				isProperty: isProperty,
				isAutoImplementedProperty: isAutoImplementedProperty
			);

			// We're manually using enumerators here.
			// - if we used IEnumerable directly we'd re-compute the first
			//   diagnostic in the GatherDiagnostics generator
			// - if we did .ToArray() early then we would avoid multiple
			//   enumeration but would compute diagnostics even when we
			//   ultimately ignore them due to annotations
			using( var diagnosticsEnumerator = diagnostics.GetEnumerator() ) {
				ProcessDiagnostics(
					context,
					diagnosticsEnumerator,
					location,
					fieldOrProperty,
					fieldOrPropertyName
				);
			}
		}
		
		private void ProcessDiagnostics(
			SyntaxNodeAnalysisContext context,
			IEnumerator<Diagnostic> diagnostics,
			Location location,
			ISymbol fieldOrProperty,
			string fieldOrPropertyName
		) {
			var hasDiagnostics = diagnostics.MoveNext();

			bool hasUnauditedAnnotation = Attributes.Statics.Unaudited.IsDefined( fieldOrProperty );
			bool hasAuditedAnnotation = Attributes.Statics.Audited.IsDefined( fieldOrProperty );

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

			bool hasAnnotations = hasAuditedAnnotation || hasUnauditedAnnotation;

			// Unnecessary annotations clutter the code base. Because
			// Statics.Audited and Statics.Unaudited enable things to build
			// that shouldn't otherwise we would like to keep the list of
			// annotations small and this covers the easy case. This also
			// provides assurance that we don't start marking things as safe
			// that we previously wouldn't due to an analyzer change (the
			// existing Statics.Audited and Statics.Unaudited serve as test
			// cases in a way.)
			if( hasAnnotations && !hasDiagnostics ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.UnnecessaryStaticAnnotation,
						location,
						ImmutableDictionary<string, string>.Empty,
						hasAuditedAnnotation ? "Statics.Audited" : "Statics.Unaudited",
						fieldOrPropertyName
					)
				);

				// this bail-out isn't important because !hasDiagnostics
				return;
			}

			if ( hasAnnotations ) {
				// the annotations supress remaining diagnostics
				return;
			}

			while ( hasDiagnostics ) {
				context.ReportDiagnostic( diagnostics.Current );
				hasDiagnostics = diagnostics.MoveNext();
			}
		}

		/// <summary>
		/// All logic relating to either emitting or not emitting diagnostics other
		/// than the ones about unnecessary annotations belong in this function.
		/// This allows InspectMember to implement the logic around the unnecessary
		/// annotations diagnostic. Any time we bail early in AnalyzeField or
		/// AnalyzeProperty we risk not emitting unnecessary annotation
		/// diagnostics.
		/// </summary>
		private IEnumerable<Diagnostic> GatherDiagnostics(
			SemanticModel model,
			MutabilityInspector inspector,
			Location location,
			bool isStatic,
			bool isReadOnly,
			ITypeSymbol fieldOrPropertyType,
			string fieldOrPropertyName,
			ExpressionSyntax initializationExpression,
			bool isProperty,
			bool isAutoImplementedProperty
		) {
			if ( !isStatic ) {
				yield break;
			}

			if( isProperty && !isAutoImplementedProperty ) {
				// non-auto-implemented properties don't hold their own state.
				// We should never emit diagnostics for them.
				yield break;
			}

			// Auto-implemented properties should be treated similar to fields:
			// 1. They should not have a setter (i.e. isReadOnly)
			// 2. Their type should be immutable
			//    a. Unless an initializer ensures it isn't

			if( !isReadOnly ) {
				yield return CreateDiagnostic(
					location,
					fieldOrPropertyName,
					fieldOrPropertyType.GetFullTypeNameWithGenericArguments(),
					MutabilityInspectionResult.Mutable(
						fieldOrPropertyName,
						fieldOrPropertyType.GetFullTypeNameWithGenericArguments(),
						MutabilityTarget.Member,
						MutabilityCause.IsNotReadonly
					)
				);

				// TODO: it'd probably be reasonable to not bail here. However
				// we need to update the unsafestatics report because it counts
				// the number of diagnostics that come out (it assumes at most one
				// error per-field.)
				yield break;
			}

			if( fieldOrPropertyType.IsTypeMarkedImmutable() ) {
				// if the type is marked immutable, skip checking it, to avoid reporting
				// a diagnostic for each usage of types that are marked
				// immutable (other analyzers catch those already.)
				yield break;
			}

			var flags = MutabilityInspectionFlags.Default;
			// Always prefer the type from the initializer if it exists because
			// it may be more specific.
			if( initializationExpression != null ) {
				var initializerType = model.GetTypeInfo( initializationExpression ).Type;

				// Fall back to the declaration type if we can't get a type for
				// the initializer.
				if( initializerType != null && !( initializerType is IErrorTypeSymbol ) ) {
					fieldOrPropertyType = initializerType;
				}
			}

			// When we know the concrete type as in "new T()" we don't have to
			// be paranoid about mutable derived classes.
			if ( initializationExpression is ObjectCreationExpressionSyntax ) {
				flags |= MutabilityInspectionFlags.AllowUnsealed;
			}

			var result = inspector.InspectType( fieldOrPropertyType, flags );
			if ( result.IsMutable ) {
				result = result.WithPrefixedMember( fieldOrPropertyName );
				yield return CreateDiagnostic( 
					location, 
					fieldOrPropertyName, 
					fieldOrPropertyType.GetFullTypeNameWithGenericArguments(), 
					result 
				);
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
