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
			ReflectionSerializer_ConstructorParameterCannotBeDeserialized,
			ReflectionSerializer_Class_NoPublicConstructor,
			ReflectionSerializer_Class_MultiplePublicConstructors,
			ReflectionSerializer_Record_NoPublicConstructor,
			ReflectionSerializer_Record_MultiplePublicConstructors,
			ReflectionSerializer_Record_InitOnlySetter,
			ReflectionSerializer_Class_Static
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

			TypeDeclaration typeDeclaration = GetTypeDeclaration( context, reflectionSerializerAttribute );

			if( type.IsStatic ) {
				ReportStaticClass( context, typeDeclaration );
				return;
			}

			AnalyzeConstructors( context, model, type, typeDeclaration );

			if( typeDeclaration.Kind == TypeDeclarationKind.Record ) {

				AnalyzeRecordProperties( context, type );
			}
		}

		private static void AnalyzeConstructors(
				SymbolAnalysisContext context,
				ReflectionSerializerModel model,
				INamedTypeSymbol type,
				TypeDeclaration typeDeclaration
			) {

			ImmutableArray<IMethodSymbol> constructors = type.InstanceConstructors;

			ImmutableArray<IMethodSymbol> publicConstructors = constructors
				.Where( c => c.DeclaredAccessibility == Accessibility.Public )
				.ToImmutableArray();

			if( publicConstructors.IsEmpty ) {

				ReportNoPublicConstructor( context, typeDeclaration );
				return;
			}

			/*
			 * The ReflectionSerializer picks the first constructor so just assuming symbols
			 * align with reflection.  In any event, we will follow up with reporting that 
			 * multiple constructors exist.
			 */
			IMethodSymbol deserializationConstructor = publicConstructors[0];
			AnalyzeConstructorParameters( context, model, type, deserializationConstructor );

			if( publicConstructors.Length > 1 ) {
				ReportMultiplePublicConstructors( context, typeDeclaration, publicConstructors );
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

				if( !serializedPropertyNames.Contains( parameter.Name ) ) {
					ReportConstructorParameterCannotBeDeserialized( context, parameter );
				}
			}
		}

		private static void ReportNoPublicConstructor(
				SymbolAnalysisContext context,
				TypeDeclaration typeDeclaration
			) {

			DiagnosticDescriptor descriptor;
			switch( typeDeclaration.Kind ) {

				case TypeDeclarationKind.Class:
					descriptor = ReflectionSerializer_Class_NoPublicConstructor;
					break;

				case TypeDeclarationKind.Record:
					descriptor = ReflectionSerializer_Record_NoPublicConstructor;
					break;

				default:
					throw new NotSupportedException( $"Unsupported type declaration kind: { typeDeclaration.Kind }" );
			}

			Diagnostic d = Diagnostic.Create(
					descriptor,
					typeDeclaration.Syntax.Identifier.GetLocation()
				);

			context.ReportDiagnostic( d );
		}

		private static void ReportMultiplePublicConstructors(
				SymbolAnalysisContext context,
				TypeDeclaration typeDeclaration,
				ImmutableArray<IMethodSymbol> constructors
			) {

			DiagnosticDescriptor descriptor;
			switch( typeDeclaration.Kind ) {

				case TypeDeclarationKind.Class:
					descriptor = ReflectionSerializer_Class_MultiplePublicConstructors;
					break;

				case TypeDeclarationKind.Record:
					descriptor = ReflectionSerializer_Record_MultiplePublicConstructors;
					break;

				default:
					throw new NotSupportedException( $"Unsupported type declaration kind: { typeDeclaration.Kind }" );
			}

			for( int i = 1; i < constructors.Length; i++ ) {

				IMethodSymbol constructor = constructors[i];
				ConstructorDeclarationSyntax declaration = GetFirstDeclaringSyntax<ConstructorDeclarationSyntax>( context, constructor );

				Diagnostic diagnostic = Diagnostic.Create(
					descriptor: descriptor,
					location: declaration.Identifier.GetLocation()
				);

				context.ReportDiagnostic( diagnostic );
			}
		}

		private static void ReportConstructorParameterCannotBeDeserialized(
				SymbolAnalysisContext context,
				IParameterSymbol parameter
			) {

			ParameterSyntax declaration = GetFirstDeclaringSyntax<ParameterSyntax>( context, parameter );

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: ReflectionSerializer_ConstructorParameterCannotBeDeserialized,
					location: declaration.Identifier.GetLocation(),
					messageArgs: new[] { parameter.Name }
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void AnalyzeRecordProperties(
				SymbolAnalysisContext context,
				INamedTypeSymbol type
			) {

			IEnumerable<IPropertySymbol> instanceProperties = type
				.GetMembers()
				.Where( m => !m.IsStatic )
				.OfType<IPropertySymbol>();

			foreach( IPropertySymbol property in instanceProperties ) {
				AnalyzeRecordProperty( context, property );
			}
		}

		private static void AnalyzeRecordProperty(
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
							ReflectionSerializer_Record_InitOnlySetter,
							accessor.Keyword.GetLocation()
						);

					context.ReportDiagnostic( d );
					return;
				}
			}
		}

		private static void ReportStaticClass(
				SymbolAnalysisContext context,
				TypeDeclaration typeDeclaration
			) {

			Diagnostic d = Diagnostic.Create(
					ReflectionSerializer_Class_Static,
					typeDeclaration.Syntax.Identifier.GetLocation()
				);

			context.ReportDiagnostic( d );
		}

		private enum TypeDeclarationKind {
			Class,
			Record
		}

		private sealed record TypeDeclaration(
				TypeDeclarationKind Kind,
				TypeDeclarationSyntax Syntax
			);

		private static TypeDeclaration GetTypeDeclaration(
				SymbolAnalysisContext context,
				AttributeData reflectionSerializerAttribute
			) {

			SyntaxNode attribute = reflectionSerializerAttribute
				.ApplicationSyntaxReference
				.GetSyntax( context.CancellationToken );

			if( !( attribute.Parent is AttributeListSyntax attributeList ) ) {
				throw new InvalidOperationException( $"Unexpected parent kind of AttributeSyntax: { attribute.Parent.Kind() }" );
			}

			switch( attributeList.Parent ) {

				case ClassDeclarationSyntax @class:
					return new( TypeDeclarationKind.Class, @class );

				case RecordDeclarationSyntax record:
					return new( TypeDeclarationKind.Record, record );

				default:
					throw new NotSupportedException( $"Unsupported [ReflectionSerializer] attribute target kind: { attributeList.Parent.Kind() }" );
			}
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
