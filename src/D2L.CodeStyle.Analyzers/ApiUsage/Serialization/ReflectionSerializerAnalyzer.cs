﻿using System;
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
		private const string DisableAnalyzerFullName = "D2L.LP.Serialization.ReflectionSerializer+DisableAnalyzerAttribute";
		private const string IgnoreAttributeFullName = "D2L.LP.Serialization.ReflectionSerializer+IgnoreAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			ReflectionSerializer_ConstructorParameterCannotBeDeserialized,
			ReflectionSerializer_Class_NoPublicConstructor,
			ReflectionSerializer_Class_MultiplePublicConstructors,
			ReflectionSerializer_Record_NoPublicConstructor,
			ReflectionSerializer_Record_MultiplePublicConstructors
		);

		private sealed record TypeDiagnostics(
				DiagnosticDescriptor NoPublicConstructor,
				DiagnosticDescriptor MultiplePublicConstructors,
				DiagnosticDescriptor ConstructorParameterCannotBeDeserialized
			);

		private static readonly TypeDiagnostics ClassDiagnostics = new TypeDiagnostics(
				NoPublicConstructor: ReflectionSerializer_Class_NoPublicConstructor,
				MultiplePublicConstructors: ReflectionSerializer_Class_MultiplePublicConstructors,
				ConstructorParameterCannotBeDeserialized: ReflectionSerializer_ConstructorParameterCannotBeDeserialized
			);

		private static readonly TypeDiagnostics RecordDiagnostics = new TypeDiagnostics(
				NoPublicConstructor: ReflectionSerializer_Record_NoPublicConstructor,
				MultiplePublicConstructors: ReflectionSerializer_Record_MultiplePublicConstructors,
				ConstructorParameterCannotBeDeserialized: ReflectionSerializer_ConstructorParameterCannotBeDeserialized
			);

		private static readonly TypeDiagnostics StructDiagnostics = new TypeDiagnostics(
				NoPublicConstructor: ReflectionSerializer_Struct_NoPublicConstructor,
				MultiplePublicConstructors: ReflectionSerializer_Struct_MultiplePublicConstructors,
				ConstructorParameterCannotBeDeserialized: ReflectionSerializer_ConstructorParameterCannotBeDeserialized
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
			if( !comp.TryGetTypeByMetadataName( DisableAnalyzerFullName, out INamedTypeSymbol disableAnalyzerAttributeType ) ) {
				disableAnalyzerAttributeType = null;
			}
			if( !comp.TryGetTypeByMetadataName( IgnoreAttributeFullName, out INamedTypeSymbol ignoreAttributeType ) ) {
				throw new InvalidOperationException( "Could not fine ReflectionSerializer.Ignore attribute type." );
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						new ReflectionSerializerModel(
							semanticModel: c.SemanticModel,
							disableAnalyzerAttributeType: disableAnalyzerAttributeType,
							ignoreAttributeType: ignoreAttributeType,
							reflectionSerializerAttributeType: reflectionSerializerAttributeType
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

				case ClassDeclarationSyntax @class:
					AnalyzeClassDeclaration( context, model, @class );
					break;

				case RecordDeclarationSyntax record:
					AnalyzeRecordDeclaration( context, model, record );
					break;
			}
		}

		private void AnalyzeClassDeclaration(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				TypeDeclarationSyntax type
			) {

			INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol( type );
			if( model.HasDisableAnalyzerAttribute( typeSymbol ) ) {
				return;
			}

			TypeDiagnostics diagnostics = ClassDiagnostics;
			bool isPartial = type.Modifiers.IndexOf( SyntaxKind.PartialKeyword ) >= 0;

			ImmutableArray<ConstructorDeclarationSyntax> constructors =
				GetPublicInstanceConstructors( type );

			ParameterListSyntax constructorParameters;

			switch( constructors.Length ) {

				case 0:
					if( !isPartial ) {
						ReportDiagnostic(
								context,
								diagnostics.NoPublicConstructor,
								type
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
							diagnostics.MultiplePublicConstructors,
							constructors.Skip( 1 )
						);
					return;
			}

			if( constructorParameters != null ) {
				AnalyzeConstructorParameters(
						context,
						model,
						type,
						typeSymbol,
						constructorParameters,
						diagnostics.ConstructorParameterCannotBeDeserialized
					);
			}

			if( isPartial ) {

				/*
				 * Only validate the constructor parameters using symbols in the event that
				 * the constructor is defined in another partial declaration
				 */
				TypeDiagnostics constructorDiagnostics = ( constructorParameters == null )
					? diagnostics
					: diagnostics with { ConstructorParameterCannotBeDeserialized = null };

				AnalyzePublicInstanceConstructors(
						context,
						model,
						type,
						typeSymbol,
						constructorDiagnostics
					);
			}
		}

		private void AnalyzeRecordDeclaration(
				SyntaxNodeAnalysisContext context,
				ReflectionSerializerModel model,
				RecordDeclarationSyntax type
			) {

			INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol( type );

			TypeDiagnostics diagnostics = RecordDiagnostics;
			bool hasPrimaryConstructor = type.ParameterList != null;
			bool isPartial = type.Modifiers.IndexOf( SyntaxKind.PartialKeyword ) >= 0;

			ImmutableArray<ConstructorDeclarationSyntax> constructors =
				GetPublicInstanceConstructors( type );

			ParameterListSyntax constructorParameters;

			if( hasPrimaryConstructor ) {

				if( constructors.Length > 0 ) {
					ReportDiagnostics(
							context,
							diagnostics.MultiplePublicConstructors,
							constructors
						);
					return;
				}

				constructorParameters = type.ParameterList;

			} else {

				switch( constructors.Length ) {

					case 0:
						if( !isPartial ) {

							ReportDiagnostic(
									context,
									diagnostics.NoPublicConstructor,
									type
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
								diagnostics.MultiplePublicConstructors,
								constructors.Skip( 1 )
							);
						return;
				}
			}

			if( constructorParameters != null ) {
				AnalyzeConstructorParameters(
						context,
						model,
						type,
						typeSymbol,
						constructorParameters,
						diagnostics.ConstructorParameterCannotBeDeserialized
					);
			}

			if( isPartial ) {

				/*
				 * Only validate the constructor parameters using symbols in the event that
				 * the constructor is defined in another partial declaration
				 */
				TypeDiagnostics constructorDiagnostics = ( constructorParameters == null )
					? diagnostics
					: diagnostics with { ConstructorParameterCannotBeDeserialized = null };

				AnalyzePublicInstanceConstructors(
						context,
						model,
						type,
						typeSymbol,
						constructorDiagnostics
					);
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
				INamedTypeSymbol typeSymbol,
				TypeDiagnostics diagnostics
			) {

			ImmutableArray<IMethodSymbol> constructors = typeSymbol
				.GetMembers()
				.OfType<IMethodSymbol>()
				.Where( IsPublicInstanceConstructor )
				.ToImmutableArray();

			switch( constructors.Length ) {

				case 0:
					ReportDiagnostic(
							context,
							diagnostics.NoPublicConstructor,
							type
						);
					break;

				case 1:
					if( diagnostics.ConstructorParameterCannotBeDeserialized != null ) {
						AnalyzeConstructorParameters(
								context,
								model,
								type,
								typeSymbol,
								constructors[ 0 ].Parameters,
								diagnostics.ConstructorParameterCannotBeDeserialized
							);
					}
					break;

				default:
					ReportDiagnostic(
							context,
							diagnostics.MultiplePublicConstructors,
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
				INamedTypeSymbol typeSymbol,
				ParameterListSyntax constructorParameters,
				DiagnosticDescriptor parameterCannotBeDeserializedDiagnostic
			) {

			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( typeSymbol );

			foreach( ParameterSyntax parameter in constructorParameters.Parameters ) {

				string parameterName = parameter.Identifier.ValueText;
				if( !serializedPropertyNames.Contains( parameterName ) ) {

					ReportConstructorParameterCannotBeDeserialized(
							context,
							descriptor: parameterCannotBeDeserializedDiagnostic,
							identifier: parameter.Identifier,
							parameterName: parameter.Identifier.ValueText
						);
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
				INamedTypeSymbol typeSymbol,
				ImmutableArray<IParameterSymbol> constructorParameters,
				DiagnosticDescriptor parameterCannotBeDeserializedDiagnostic
			) {

			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( typeSymbol );

			foreach( IParameterSymbol parameter in constructorParameters ) {

				string parameterName = parameter.Name;
				if( !serializedPropertyNames.Contains( parameterName ) ) {

					ReportConstructorParameterCannotBeDeserialized(
							context,
							descriptor: parameterCannotBeDeserializedDiagnostic,
							identifier: type.Identifier,
							parameterName: parameter.Name
						);
				}
			}
		}

		private static void ReportConstructorParameterCannotBeDeserialized(
				SyntaxNodeAnalysisContext context,
				DiagnosticDescriptor descriptor,
				SyntaxToken identifier,
				string parameterName
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: identifier.GetLocation(),
					messageArgs: parameterName
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
