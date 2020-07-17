using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReflectionSerializerConstructorAnalyzer : DiagnosticAnalyzer {

		private const string ReflectionSerializerAttributeFullName = "D2L.LP.Serialization.ReflectionSerializerAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReflectionSerializer_NoSinglePublicConstructor
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

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						reflectionSerializerAttributeType
					),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttributeSyntax(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol reflectionSerializerAttributeType
			) {

			SemanticModel model = context.SemanticModel;
			if( !model.IsAttributeOfType( attributeSyntax, reflectionSerializerAttributeType ) ) {
				return;
			}

			if( !TryGetAttributeTarget( attributeSyntax, out TypeDeclarationSyntax typeDeclarationSyntax ) ) {
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

			ImmutableArray<ConstructorDeclarationSyntax> publicConstructors = constructors
				.Where( c => c.IsPublic() )
				.ToImmutableArray();

			if( publicConstructors.Length == 1 ) {
				return;
			}

			ReportNoSinglePublicConstructor(
					context,
					typeDeclarationSyntax
				);
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

		private void ReportNoSinglePublicConstructor(
				SyntaxNodeAnalysisContext context,
				TypeDeclarationSyntax typeDeclarationSyntax
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: Diagnostics.ReflectionSerializer_NoSinglePublicConstructor,
					location: typeDeclarationSyntax.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
