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

			context.RegisterSemanticModelAction(
					c => AnalyzeSemanticModel(
						c,
						new ReflectionSerializerModel(
							c.SemanticModel,
							reflectionSerializerAttributeType,
							ignoreAttributeType
						)
					)
				);
		}

		private void AnalyzeSemanticModel(
				SemanticModelAnalysisContext context,
				ReflectionSerializerModel model
			) {

			SyntaxNode root = context.SemanticModel.SyntaxTree.GetRoot();

			IEnumerable<TypeDeclarationSyntax> typeDeclarations = root
				.DescendantNodes( DescendIntoTypeDeclarations, descendIntoTrivia: false )
				.Where( IsReflectionSerializerTargetTypeDeclaration )
				.Cast<TypeDeclarationSyntax>();

			foreach( TypeDeclarationSyntax typeDeclaration in typeDeclarations ) {

				if( !model.DefinesReflectionSerializerAttribute( typeDeclaration ) ) {
					continue;
				}

				switch( typeDeclaration ) {

					case RecordDeclarationSyntax @record:
						AnalyzeRecordDeclarationSyntax( context, model, record );
						break;

					default:
						break;
				}
			}
		}

		private static bool DescendIntoTypeDeclarations( SyntaxNode node ) {

			SyntaxKind kind = node.Kind();
			switch( kind ) {

				case SyntaxKind.CompilationUnit:
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.IndexerDeclaration:
				case SyntaxKind.NamespaceDeclaration:
				case SyntaxKind.RecordDeclaration:
				case SyntaxKind.StructDeclaration:
					return true;

				default:
					return false;
			}
		}

		private static bool IsReflectionSerializerTargetTypeDeclaration( SyntaxNode node ) {

			switch( node.Kind() ) {

				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.RecordDeclaration:
				case SyntaxKind.StructDeclaration:
					return true;

				default:
					return false;
			}
		}

		private void AnalyzeRecordDeclarationSyntax(
				SemanticModelAnalysisContext context,
				ReflectionSerializerModel model,
				RecordDeclarationSyntax record
			) {

			bool hasPrimaryConstructor = record.ParameterList != null;
			ImmutableArray<ConstructorDeclarationSyntax> constructors = GetPublicInstanceConstructors( record );

			ParameterListSyntax constructorParameters;

			if( hasPrimaryConstructor ) {

				if( constructors.Length > 0 ) {
					ReportMultipleRecordPublicConstructors( context, constructors );
					return;
				}

				constructorParameters = record.ParameterList;

			} else {

				if( constructors.Length == 0 ) {
					ReportNoPublicRecordConstructor( context, record );
					return;
				}

				if( constructors.Length > 1 ) {
					ReportMultipleRecordPublicConstructors( context, constructors.Skip( 1 ) );
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

		private static void ReportNoPublicRecordConstructor(
				SemanticModelAnalysisContext context,
				RecordDeclarationSyntax record
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_Record_NoPublicConstructor,
					location: record.Identifier.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void ReportMultipleRecordPublicConstructors(
				SemanticModelAnalysisContext context,
				IEnumerable<ConstructorDeclarationSyntax> constructors
			) {

			foreach( ConstructorDeclarationSyntax constructor in constructors ) {

				Diagnostic diagnostic = Diagnostic.Create(
						descriptor: ReflectionSerializer_Record_MultiplePublicConstructors,
						location: constructor.Identifier.GetLocation()
					);

				context.ReportDiagnostic( diagnostic );
			}
		}

		private static void ReportConstructorParameterCannotBeDeserialized(
				SemanticModelAnalysisContext context,
				ParameterSyntax parameter
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_Record_ConstructorParameterCannotBeDeserialized,
					location: parameter.Identifier.GetLocation(),
					messageArgs: parameter.Identifier.ValueText
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static ImmutableArray<ConstructorDeclarationSyntax> GetPublicInstanceConstructors(
				TypeDeclarationSyntax syntax
			) {

			ImmutableArray<ConstructorDeclarationSyntax> constructors = syntax
				.ChildNodes()
				.OfType<ConstructorDeclarationSyntax>()
				.Where( c => c.IsPublic() && !c.IsStatic() )
				.ToImmutableArray();

			return constructors;
		}
	}
}
