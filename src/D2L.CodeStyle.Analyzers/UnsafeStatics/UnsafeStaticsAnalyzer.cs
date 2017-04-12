using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.UnsafeStatics {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class UnsafeStaticsAnalyzer : DiagnosticAnalyzer {

		public const string PROPERTY_FIELDORPROPNAME = "FieldOrProprName";
		public const string PROPERTY_OFFENDINGTYPE = "OffendingType";

		public const string DiagnosticId = "D2L0002";
		private const string Category = "Safety";

		private const string Title = "Ensure that static field is safe in undifferentiated servers.";
		private const string Description = "Static fields should not have client-specific or mutable data, otherwise they will not be safe in undifferentiated servers.";
		internal const string MessageFormat = "The static field or property '{0}' is unsafe because {1}.";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

		private readonly MutabilityInspector m_immutabilityInspector = new MutabilityInspector();
		private readonly Utils m_utils = new Utils();
		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationStartAction( ctx => {
				if( ShouldAnalyzeCompilation( ctx.Compilation ) ) {
					ctx.RegisterSyntaxNodeAction( AnalyzeField, SyntaxKind.FieldDeclaration );
					ctx.RegisterSyntaxNodeAction( AnalyzeProperty, SyntaxKind.PropertyDeclaration );
				}
			} );
		}

		private bool ShouldAnalyzeCompilation( Compilation compilation ) {
			var assemblyName = compilation.AssemblyName;
			if( assemblyName.Contains( "Test" ) ) {
				// Compilation is a test assembly, skip
				return false;
			}
			var references = compilation.ReferencedAssemblyNames;
			if( references.Any( r => r.Name.ToUpper().Contains( "NUNIT" ) ) ) {
				// Compilation is a test assembly, skip
				return false;
			}

			var attributes = compilation.Assembly.GetAttributes();
			if( attributes.Any( a => a.AttributeClass.MetadataName == "SuperHackySketchyAssemblyThatIsExemptCuzLikeItsSpecialSnowflake" ) ) {
				// bail out on assemblies with this attribute
				return false;
			}

			return true;
		}

		private void AnalyzeField( SyntaxNodeAnalysisContext context ) {
			if( m_utils.IsGeneratedCodefile( context.Node.SyntaxTree.FilePath ) ) {
				// skip code-gen'd files; they have been hand-inspected to be safe
				return;
			}

			var root = context.Node as FieldDeclarationSyntax;
			if( root == null ) {
				return;
			}

			if( !root.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
				// ignore non-static
				return;
			}

			foreach( var variable in root.Declaration.Variables ) {
				var symbol = context.SemanticModel.GetDeclaredSymbol( variable ) as IFieldSymbol;

				if( symbol == null ) {
					continue;
				}

				if( symbol.GetAttributes().Any( a => a.AttributeClass.MetadataName == "Unaudited" ) ) {
					// anyhing marked unaudited should not break the build, it's temporary
					return;
				}
				if( symbol.GetAttributes().Any( a => a.AttributeClass.MetadataName == "Audited" ) ) {
					// anything marked audited has been explicitly marked as a safe static
					return;
				}

				if( m_immutabilityInspector.IsFieldMutable( symbol ) ) {
					var diagnostic = CreateDiagnostic(
						variable.GetLocation(),
						symbol.Name,
						symbol.Type.GetFullTypeNameWithGenericArguments(),
						MutabilityInspectionResult.Mutable(
							symbol.Name,
							symbol.Type.GetFullTypeNameWithGenericArguments(),
							MutabilityTarget.Member,
							MutabilityCause.IsNotReadonly
						)
					);
					context.ReportDiagnostic( diagnostic );
					return;
				}

				InspectType(
					context,
					symbol.Type,
					variable.Initializer?.Value,
					variable.GetLocation(),
					variable.Identifier.ValueText
				);
			}
		}

		private void AnalyzeProperty( SyntaxNodeAnalysisContext context ) {
			if( m_utils.IsGeneratedCodefile( context.Node.SyntaxTree.FilePath ) ) {
				// skip code-gen'd files; they have been hand-inspected to be safe
				return;
			}

			var root = context.Node as PropertyDeclarationSyntax;
			if( root == null ) {
				return;
			}

			if( !root.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
				// ignore non-static
				return;
			}

			var prop = context.SemanticModel.GetDeclaredSymbol( root );
			if( prop == null ) {
				return;
			}

#pragma warning disable CS0618 // Type or member is obsolete
			if( prop.GetAttributes().Any( a => a.AttributeClass.MetadataName == "Unaudited" ) ) {
				// anyhing marked unaudited should not break the build, it's temporary
				return;
			}
#pragma warning restore CS0618 // Type or member is obsolete

			if( prop.GetAttributes().Any( a => a.AttributeClass.MetadataName == "Audited" ) ) {
				// anything marked audited has been explicitly marked as a safe static
				return;
			}

			if( m_immutabilityInspector.IsPropertyMutable( prop ) ) {
				var diagnostic = CreateDiagnostic(
					root.GetLocation(),
					prop.Name,
					prop.Type.GetFullTypeNameWithGenericArguments(),
					MutabilityInspectionResult.Mutable(
						prop.Name,
						prop.Type.GetFullTypeNameWithGenericArguments(),
						MutabilityTarget.Member,
						MutabilityCause.IsNotReadonly
					)
				);
				context.ReportDiagnostic( diagnostic );
				return;
			}
			if( root.IsPropertyGetterImplemented() ) {
				// property has getter with body; it is either backed by a field, or is a static function; ignore
				return;
			}

			InspectType( context, prop.Type, root.Initializer?.Value, root.GetLocation(), prop.Name );
		}

		private void InspectType(
			SyntaxNodeAnalysisContext context,
			ITypeSymbol type,
			ExpressionSyntax exp,
			Location location,
			string fieldOrPropName
		) {
			if( m_immutabilityInspector.IsTypeMarkedImmutable( type ) ) {
				// if the type is marked immutable, skip checking it, to avoid reporting a diagnostic for each usage of non-immutable types that are marked immutable (another analyzer catches this already)
				return;
			}

			var flags = MutabilityInspectionFlags.Default;

			// Always prefer the type from the initializer if it exists because
			// it may be more specific.
			if( exp != null ) {
				var typeInfo = context.SemanticModel.GetTypeInfo( exp );

				// Fall back to the declaration type if we can't get a type for
				// the initializer
				if( typeInfo.Type != null && !( typeInfo.Type is IErrorTypeSymbol ) ) {
					type = typeInfo.Type;
				}
			}

			// When we know the concrete type as in "new T()" we don't have to
			// be paranoid about mutable derived classes
			if ( exp is ObjectCreationExpressionSyntax ) {
				flags |= MutabilityInspectionFlags.AllowUnsealed;
			}

			var result = m_immutabilityInspector.InspectType( type, flags );
			if ( result.IsMutable ) {
				result = result.WithPrefixedMember( fieldOrPropName );
				var diagnostic = CreateDiagnostic( 
					location, 
					fieldOrPropName, 
					type.GetFullTypeNameWithGenericArguments(), 
					result 
				);
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
				Rule,
				location,
				properties,
				fieldOrPropName,
				reason
			);
			return diagnostic;
		}

	}
}
