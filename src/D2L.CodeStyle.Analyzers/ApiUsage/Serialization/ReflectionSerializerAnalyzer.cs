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
			ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error
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
					AnalyzeRecordDeclaration( context, model, record );
					break;
			}
		}

		private void AnalyzeRecordDeclaration(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				RecordDeclarationSyntax record
			) {

			bool hasPrimaryConstructor = record.ParameterList != null;
			bool isPartial = record.Modifiers.IndexOf( SyntaxKind.PartialKeyword ) >= 0;

			ImmutableArray<ConstructorDeclarationSyntax> constructors =
				GetPublicInstanceConstructors( record );

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

				switch( constructors.Length ) {

					case 0:
						if( !isPartial ) {

							ReportDiagnostic(
									context,
									ReflectionSerializer_Record_NoPublicConstructor,
									record
								);
							return;
						}

						constructorParameters = null;
						break;

					case 1:
						constructorParameters = constructors[ 0 ].ParameterList;
						break;

					default:
						ReportDiagnostics(
								context,
								ReflectionSerializer_Record_MultiplePublicConstructors,
								constructors.Skip( 1 )
							);
						return;
				}
			}

			if( constructorParameters != null ) {
				AnalyzeConstructorParameters(
						context,
						model,
						record,
						constructorParameters,
						asErrors: true
					);
			}

			if( isPartial ) {

				/*
				 * Only validate the constructor parameters using symbols in the event that
				 * the constructor is defined in another partial declaration
				 */
				ConstructorParameterValidation parameterValidation = constructorParameters == null
					? ConstructorParameterValidation.AsErrors
					: ConstructorParameterValidation.None;

				AnalyzePublicInstanceConstructors(
						context,
						model,
						record,
						parameterValidation
					);
			}
		}

		private enum ConstructorParameterValidation {
			None,
			AsErrors,
			AsWarnings
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

		private static ImmutableArray<ConstructorDeclarationSyntax> GetPublicInstanceConstructors(
				TypeDeclarationSyntax syntax
			) {

			ImmutableArray<ConstructorDeclarationSyntax> constructors = syntax
				.ChildNodes()
				.OfType<ConstructorDeclarationSyntax>()
				.Where( c => (
					c.Modifiers.IndexOf( SyntaxKind.PublicKeyword ) >= 0
					& c.Modifiers.IndexOf( SyntaxKind.StaticKeyword ) < 0
				) )
				.ToImmutableArray();

			return constructors;
		}

		private static bool IsPublicInstanceConstructor( IMethodSymbol method ) {

			if( method.MethodKind != MethodKind.Constructor ) {
				return false;
			}

			if( method.DeclaredAccessibility != Accessibility.Public ) {
				return false;
			}

			return true;
		}

		private static void AnalyzePublicInstanceConstructors(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				TypeDeclarationSyntax type,
				ConstructorParameterValidation paramcterValidation
			) {

			INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol( type );

			ImmutableArray<IMethodSymbol> constructors = typeSymbol
				.GetMembers()
				.OfType<IMethodSymbol>()
				.Where( IsPublicInstanceConstructor )
				.ToImmutableArray();

			switch( constructors.Length ) {

				case 0:
					ReportDiagnostic(
							context,
							ReflectionSerializer_Record_NoPublicConstructor,
							type
						);
					break;

				case 1:

					switch( paramcterValidation ) {

						case ConstructorParameterValidation.AsErrors:
							AnalyzeConstructorParameters(
									context,
									model,
									type,
									constructors[ 0 ].Parameters,
									asErrors: true
								);
							break;

						case ConstructorParameterValidation.AsWarnings:
							AnalyzeConstructorParameters(
									context,
									model,
									type,
									constructors[ 0 ].Parameters,
									asErrors: false
								);
							break;
					}
					break;

				default:
					ReportDiagnostic(
							context,
							ReflectionSerializer_Record_MultiplePublicConstructors,
							type
						);
					break;
			}
		}

		/// <summary>
		/// Analyzes the constructor parameters using the syntax in the event of a partial type,
		/// where the [ReflectionSerializer] attribute and constructor are defined in the same file.
		/// </summary>
		private static void AnalyzeConstructorParameters(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				TypeDeclarationSyntax type,
				ParameterListSyntax constructorParameters,
				bool asErrors
			) {

			INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol( type );
			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( typeSymbol );

			foreach( ParameterSyntax parameter in constructorParameters.Parameters ) {

				string parameterName = parameter.Identifier.ValueText;
				if( !serializedPropertyNames.Contains( parameterName ) ) {

					DiagnosticDescriptor descriptor = asErrors
						? ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error
						: ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Warning;

					Diagnostic diagnostic = Diagnostic.Create(
							descriptor: descriptor,
							location: parameter.Identifier.GetLocation(),
							messageArgs: parameter.Identifier.ValueText
						);

					context.ReportDiagnostic( diagnostic );
				}
			}
		}

		/// <summary>
		/// Analyzes the constructor parameters using the symbols in the event of a partial type,
		/// where the [ReflectionSerializer] attribute and constructor are not in the same file.
		/// </summary>
		private static void AnalyzeConstructorParameters(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				TypeDeclarationSyntax type,
				ImmutableArray<IParameterSymbol> constructorParameters,
				bool asErrors
			) {

			INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol( type );
			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( typeSymbol );

			foreach( IParameterSymbol parameter in constructorParameters ) {

				string parameterName = parameter.Name;
				if( !serializedPropertyNames.Contains( parameterName ) ) {

					DiagnosticDescriptor descriptor = asErrors
						? ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error
						: ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Warning;

					Diagnostic diagnostic = Diagnostic.Create(
							descriptor: descriptor,
							location: type.Identifier.GetLocation(),
							messageArgs: parameter.Name
						);

					context.ReportDiagnostic( diagnostic );
				}
			}
		}
	}
}
