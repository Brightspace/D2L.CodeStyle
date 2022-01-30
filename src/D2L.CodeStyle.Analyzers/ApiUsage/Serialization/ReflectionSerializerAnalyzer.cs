#nullable disable

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
			ReflectionSerializer_ConstructorParameter_CannotBeDeserialized,
			ReflectionSerializer_ConstructorParameter_InvalidRefKind,
			ReflectionSerializer_InitOnlySetter,
			ReflectionSerializer_MultiplePublicConstructors,
			ReflectionSerializer_NoPublicConstructor,
			ReflectionSerializer_StaticClass
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private void CompilationStart( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( ReflectionSerializerAttributeFullName, out INamedTypeSymbol reflectionSerializerAttributeType ) ) {
				return;
			}
			if( !comp.TryGetTypeByMetadataName( IgnoreAttributeFullName, out INamedTypeSymbol ignoreAttributeType ) ) {
				throw new InvalidOperationException( "Could not fine ReflectionSerializer.Ignore attribute type." );
			}

			ReflectionSerializerModel model = new ReflectionSerializerModel(
					ignoreAttributeType: ignoreAttributeType,
					reflectionSerializerAttributeType: reflectionSerializerAttributeType
				);

			context.RegisterSymbolAction(
					c => AnalyzeAttributeSyntax( c, model, (INamedTypeSymbol)c.Symbol ),
					SymbolKind.NamedType
				);
		}

		private static void AnalyzeAttributeSyntax(
				SymbolAnalysisContext context,
				ReflectionSerializerModel model,
				INamedTypeSymbol type
			) {

			if( type.TypeKind != TypeKind.Class ) {
				return;
			}

			if( type.IsAbstract ) {
				return;
			}

			if( !model.HasReflectionSerializerAttribute( type, out AttributeData reflectionSerializerAttribute ) ) {
				return;
			}

			if( type.IsStatic ) {
				ReportStaticClass( context, reflectionSerializerAttribute );
				return;
			}

			AnalyzeConstructors( context, model, reflectionSerializerAttribute, type );
			AnalyzeProperties( context, type );
		}

		private static void AnalyzeConstructors(
				SymbolAnalysisContext context,
				ReflectionSerializerModel model,
				AttributeData reflectionSerializerAttribute,
				INamedTypeSymbol type
			) {

			ImmutableArray<IMethodSymbol> publicConstructors = model
				.GetOrderedPublicInstanceConstructors( type );

			if( publicConstructors.IsEmpty ) {

				ReportNoPublicConstructor( context, reflectionSerializerAttribute );
				return;
			}

			/*
			 * The ReflectionSerializer picks the first constructor so just assuming symbols
			 * align with reflection.  In any event, we will follow up with reporting that 
			 * multiple constructors exist.
			 */
			IMethodSymbol deserializationConstructor = publicConstructors[0];
			AnalyzeConstructorParameters( context, model, type, deserializationConstructor );

			for( int i = 1; i < publicConstructors.Length; i++ ) {
				ReportMultiplePublicConstructors( context, publicConstructors[ i ] );
			}
		}

		private static void AnalyzeConstructorParameters(
				SymbolAnalysisContext context,
				ReflectionSerializerModel model,
				INamedTypeSymbol type,
				IMethodSymbol constructor
			) {

			ImmutableArray<IParameterSymbol> constructorParameters = constructor.Parameters;
			if( constructorParameters.IsEmpty ) {
				return;
			}

			ImmutableHashSet<string> serializedPropertyNames = model.GetPublicReadablePropertyNames( type );

			foreach( IParameterSymbol parameter in constructorParameters ) {

				RefKind refKind = parameter.RefKind;
				if( refKind == RefKind.None ) {

					if( !serializedPropertyNames.Contains( parameter.Name ) ) {
						ReportConstructorParameterCannotBeDeserialized( context, parameter );
					}

				} else {
					ReportConstructorParameterInvalidRefKind( context, parameter, refKind );
				}
			}
		}

		private static void ReportNoPublicConstructor(
				SymbolAnalysisContext context,
				AttributeData reflectionSerializerAttribute
			) {

			TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration( context, reflectionSerializerAttribute );

			Diagnostic d = Diagnostic.Create(
					ReflectionSerializer_NoPublicConstructor,
					typeDeclaration.Identifier.GetLocation()
				);

			context.ReportDiagnostic( d );
		}

		private static void ReportMultiplePublicConstructors(
				SymbolAnalysisContext context,
				IMethodSymbol constructor
			) {

			ConstructorDeclarationSyntax declaration = GetFirstDeclaringSyntax<ConstructorDeclarationSyntax>( context, constructor );

			Diagnostic diagnostic = Diagnostic.Create(
				descriptor: ReflectionSerializer_MultiplePublicConstructors,
				location: declaration.Identifier.GetLocation()
			);

			context.ReportDiagnostic( diagnostic );
		}

		private static void ReportConstructorParameterCannotBeDeserialized(
				SymbolAnalysisContext context,
				IParameterSymbol parameter
			) {

			ParameterSyntax declaration = GetFirstDeclaringSyntax<ParameterSyntax>( context, parameter );

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_ConstructorParameter_CannotBeDeserialized,
					location: declaration.Identifier.GetLocation(),
					messageArgs: new[] { parameter.Name }
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void ReportConstructorParameterInvalidRefKind(
				SymbolAnalysisContext context,
				IParameterSymbol parameter,
				RefKind refKind
			) {

			ParameterSyntax declaration = GetFirstDeclaringSyntax<ParameterSyntax>( context, parameter );

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_ConstructorParameter_InvalidRefKind,
					location: declaration.Identifier.GetLocation(),
					messageArgs: new[] {
						parameter.Name,
						refKind.ToString().ToLowerInvariant()
					}
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void AnalyzeProperties(
				SymbolAnalysisContext context,
				INamedTypeSymbol type
			) {

			IEnumerable<IPropertySymbol> instanceProperties = type
				.GetMembers()
				.Where( m => !m.IsStatic )
				.OfType<IPropertySymbol>();

			foreach( IPropertySymbol property in instanceProperties ) {
				AnalyzeProperty( context, property );
			}
		}

		private static void AnalyzeProperty(
				SymbolAnalysisContext context,
				IPropertySymbol property
			) {

			if( property.DeclaringSyntaxReferences.IsEmpty ) {
				return;
			}

			SyntaxNode declaringSyntax = property
				.DeclaringSyntaxReferences[0]
				.GetSyntax( context.CancellationToken );

			if( !( declaringSyntax is PropertyDeclarationSyntax declaration ) ) {
				return;
			}

			AccessorListSyntax accessors = declaration.AccessorList;
			if( accessors == null ) {
				return;
			}

			for( int i = 0; i < accessors.Accessors.Count; i++ ) {

				AccessorDeclarationSyntax accessor = accessors.Accessors[i];
				if( accessor.Kind() == SyntaxKind.InitAccessorDeclaration ) {

					Diagnostic d = Diagnostic.Create(
							ReflectionSerializer_InitOnlySetter,
							accessor.Keyword.GetLocation()
						);

					context.ReportDiagnostic( d );
					return;
				}
			}
		}

		private static void ReportStaticClass(
				SymbolAnalysisContext context,
				AttributeData reflectionSerializerAttribute
			) {

			TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration( context, reflectionSerializerAttribute );

			Diagnostic d = Diagnostic.Create(
					ReflectionSerializer_StaticClass,
					typeDeclaration.Identifier.GetLocation()
				);

			context.ReportDiagnostic( d );
		}

		private static TypeDeclarationSyntax GetTypeDeclaration(
				SymbolAnalysisContext context,
				AttributeData reflectionSerializerAttribute
			) {

			SyntaxNode attribute = reflectionSerializerAttribute
				.ApplicationSyntaxReference
				.GetSyntax( context.CancellationToken );

			if( !( attribute.Parent is AttributeListSyntax attributeList ) ) {
				throw new InvalidOperationException( $"Unexpected parent kind of AttributeSyntax: { attribute.Parent.Kind() }" );
			}

			if( !( attributeList.Parent is TypeDeclarationSyntax typeDeclaration ) ) {
				throw new InvalidOperationException( $"Unsupported [ReflectionSerializer] attribute target kind: { attributeList.Parent.Kind() }" );
			}

			return typeDeclaration;
		}

		private static T GetFirstDeclaringSyntax<T>(
				SymbolAnalysisContext context,
				ISymbol symbol
			) where T : SyntaxNode {

			SyntaxNode syntax = symbol.DeclaringSyntaxReferences
				.First()
				.GetSyntax( context.CancellationToken );

			return (T)syntax;
		}
	}
}
