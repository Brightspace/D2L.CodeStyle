using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static D2L.CodeStyle.Analyzers.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ReflectionSerializerAnalyzer : DiagnosticAnalyzer {

		private const string ReflectionSerializerAttributeFullName = "D2L.LP.Serialization.ReflectionSerializerAttribute";
		private const string IgnoreAttributeFullName = "D2L.LP.Serialization.ReflectionSerializer+IgnoreAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			ReflectionSerializer_Record_NoPublicConstructor,
			ReflectionSerializer_Record_MultiplePublicConstructors,
			ReflectionSerializer_Record_ConstructorParameterCannotBeDeserialized
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterScopeBuilderAnalyzer );
		}

		private void RegisterScopeBuilderAnalyzer( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( ReflectionSerializerAttributeFullName, out INamedTypeSymbol reflectionSerializerAttributeType ) ) {
				return;
			}
			if( !comp.TryGetTypeByMetadataName( IgnoreAttributeFullName, out INamedTypeSymbol ignoreAttributeType ) ) {
				throw new InvalidOperationException( "Could not fine ReflectionSerializer.Ignore attribute type." );
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						new ReflectionSerializerModel(
							c.SemanticModel,
							reflectionSerializerAttributeType,
							ignoreAttributeType
						)
					),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttributeSyntax(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attribute,
				ReflectionSerializerModel model
			) {

			if( !model.IsReflectionSerializerAttribute( attribute ) ) {
				return;
			}

			if( !( attribute.Parent is AttributeListSyntax attributeList ) ) {
				return;
			}

			switch( attributeList.Parent ) {

				case RecordDeclarationSyntax record:
					AnalyzeRecordDeclarationSyntax( context, model, record );
					break;
			}
		}

		private void AnalyzeRecordDeclarationSyntax(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				RecordDeclarationSyntax record
			) {

			bool hasPrimaryConstructor = record.ParameterList != null;

			ImmutableArray<ConstructorDeclarationSyntax> constructors =
				model.GetPublicInstanceConstructors( record );

			ParameterListSyntax constructorParameters;

			if( hasPrimaryConstructor ) {

				if( constructors.Length > 0 ) {
					ReportDiagnostics(
							context,
							ReflectionSerializer_Record_MultiplePublicConstructors,
							constructors
						);
					return;
				}

				constructorParameters = record.ParameterList;

			} else {

				if( constructors.Length == 0 ) {
					ReportDiagnostic(
							context,
							ReflectionSerializer_Record_NoPublicConstructor,
							record
						);
					return;
				}

				if( constructors.Length > 1 ) {
					ReportDiagnostics(
							context,
							ReflectionSerializer_Record_MultiplePublicConstructors,
							constructors.Skip( 1 )
						);
					return;
				}

				constructorParameters = constructors[ 0 ].ParameterList;
			}

			INamedTypeSymbol recordType = context.SemanticModel.GetDeclaredSymbol( record );
			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( recordType );

			foreach( ParameterSyntax parameter in constructorParameters.Parameters ) {
				string parameterName = parameter.Identifier.ValueText;

				if( !serializedPropertyNames.Contains( parameterName ) ) {
					ReportConstructorParameterCannotBeDeserialized( context, parameter );
				}
			}
		}

		private static void ReportDiagnostic(
				SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				TypeDeclarationSyntax type
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: type.Identifier.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void ReportDiagnostics(
				SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				IEnumerable<ConstructorDeclarationSyntax> constructors
			) {

			foreach( ConstructorDeclarationSyntax constructor in constructors ) {

				Diagnostic diagnostic = Diagnostic.Create(
						descriptor: descriptor,
						location: constructor.Identifier.GetLocation()
					);

				context.ReportDiagnostic( diagnostic );
			}
		}

		private static void ReportConstructorParameterCannotBeDeserialized(
				SyntaxNodeAnalysisContext context,
				ParameterSyntax parameter
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_Record_ConstructorParameterCannotBeDeserialized,
					location: parameter.Identifier.GetLocation(),
					messageArgs: parameter.Identifier.ValueText
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
