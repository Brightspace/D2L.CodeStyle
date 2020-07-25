using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReflectionSerializerAnalyzer : DiagnosticAnalyzer {

		private const string ReflectionSerializerAttributeFullName = "D2L.LP.Serialization.ReflectionSerializerAttribute";
		private const string ConstructorAttributeFullName = "D2L.LP.Serialization.ReflectionSerializer+ConstructorAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReflectionSerializer_NoPublicConstructors,
			Diagnostics.ReflectionSerializer_MultiplePublicConstructors,
			Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyPublic,
			Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyFirstPublic,
			Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyIfMultiplePublic
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterScopeBuilderAnalyzer );
		}

		private void RegisterScopeBuilderAnalyzer( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( ReflectionSerializerAttributeFullName, out INamedTypeSymbol reflectionSerializerAttributeType ) ) {
				return;
			}

			if( comp.TryGetTypeByMetadataName( ConstructorAttributeFullName, out INamedTypeSymbol constructorAttributeType ) ) {
				constructorAttributeType = null;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						reflectionSerializerAttributeType,
						constructorAttributeType
					),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttributeSyntax(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol reflectionSerializerAttributeType,
				INamedTypeSymbol constructorAttributeType
			) {

			SemanticModel model = context.SemanticModel;
			if( !model.IsAttributeOfType( attributeSyntax, reflectionSerializerAttributeType ) ) {
				return;
			}

			if( !TryGetAttributeTarget( attributeSyntax, out TypeDeclarationSyntax typeDeclarationSyntax ) ) {
				return;
			}

			if( typeDeclarationSyntax.Modifiers.IndexOf( SyntaxKind.AbstractKeyword ) >= 0 ) {
				return;
			}

			ImmutableArray<ConstructorDeclarationSyntax> constructors = typeDeclarationSyntax
				.ChildNodes()
				.OfType<ConstructorDeclarationSyntax>()
				.Where( c => !c.IsStatic() )
				.ToImmutableArray();

			if( constructors.Length == 0 ) {

				// default constructor
				return;
			}

			// ---------------------------------------------------------------------------------------------

			List<ConstructorDeclarationSyntax> publicConstructors = new List<ConstructorDeclarationSyntax>();
			ConstructorDeclarationSyntax attributedPublicConstructor = null;

			foreach( ConstructorDeclarationSyntax constructor in constructors ) {

				bool hasConstructorAttribute = HasConstructorAttribute( model, constructor, constructorAttributeType );

				if( constructor.IsPublic() ) {

					if( hasConstructorAttribute  ) {

						if( publicConstructors.Count == 0 ) {
							attributedPublicConstructor = constructor;

						} else {

							// only the first public constructor can be attributed with [ReflectionSerializer.Constructor]
							ReportConstructorDiagnostic(
									context,
									Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyFirstPublic,
									constructor
								);
						}
					}

					publicConstructors.Add( constructor );

				} else if( hasConstructorAttribute ) {

					// only the public constructors can be attributed with [ReflectionSerializer.Constructor]
					ReportConstructorDiagnostic(
							context,
							Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyPublic,
							constructor
						);
				}
			}

			if( attributedPublicConstructor  != null ) {

				if( publicConstructors.Count == 1 ) {

					// [ReflectionSerializer.Constructor] is only required if multiple public constructors exist
					ReportConstructorDiagnostic(
							context,
							Diagnostics.ReflectionSerializer_ConstructorAttribute_OnlyIfMultiplePublic,
							attributedPublicConstructor
						);
				}

				return;
			}

			// ---------------------------------------------------------------------------------------------

			switch( publicConstructors.Count ) {

				case 0:

					ReportTargetTypeDiagnostic(
							context,
							Diagnostics.ReflectionSerializer_NoPublicConstructors,
							typeDeclarationSyntax
						);
					return;

				case 1:
					return;

				default:

					ReportTargetTypeDiagnostic(
							context,
							Diagnostics.ReflectionSerializer_MultiplePublicConstructors,
							typeDeclarationSyntax
						);
					return;
			}
		}

		private static bool TryGetAttributeTarget(
				AttributeSyntax attributeSyntax,
				out TypeDeclarationSyntax target
			) {

			if( !( attributeSyntax.Parent is AttributeListSyntax attributeListSyntax ) ) {
				target = null;
				return false;
			}

			switch( attributeListSyntax.Parent ) {

				case ClassDeclarationSyntax @class:
					target = @class;
					return true;

				case StructDeclarationSyntax @struct:
					target = @struct;
					return true;

				default:
					target = null;
					return false;
			}
		}

		private static void ReportTargetTypeDiagnostic(
				SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				TypeDeclarationSyntax typeDeclarationSyntax
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: typeDeclarationSyntax.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void ReportConstructorDiagnostic(
				SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				ConstructorDeclarationSyntax constructorSyntax
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: constructorSyntax.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static bool HasConstructorAttribute(
				SemanticModel model,
				ConstructorDeclarationSyntax constructor,
				INamedTypeSymbol constructorAttributeType
			) {

			IEnumerable<AttributeListSyntax> attributeLists = constructor
				.ChildNodes()
				.OfType<AttributeListSyntax>();

			foreach( AttributeListSyntax attributeList in attributeLists ) {

				if( attributeList.Attributes == null ) {
					continue;
				}

				foreach( AttributeSyntax attr in attributeList.Attributes ) {

					if( model.IsAttributeOfType( attr, constructorAttributeType )  ) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
