using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class RecursivelyDeserializableAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.ReflectionSerializerDescendantsMustBeDeserializable
			);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			var deserializableType = DeserializableAnalyzerHelper.GetDeserializableTypeInfo( context.Compilation );
			if( deserializableType == null ) {
				return;
			}
			context.RegisterSyntaxNodeAction( ( ctx ) => AnalyzeSyntaxNode( ctx, context.Options.AdditionalFiles, deserializableType ),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.RecordDeclaration,
				SyntaxKind.StructDeclaration );
		}

		private static void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context, ImmutableArray<AdditionalText> additionalTexts, DeserializableTypeInfo deserializableType ) {
			var baseDeclaration = context.Node as TypeDeclarationSyntax;
			if( baseDeclaration == null ) {
				return;
			}

			var reflectionSerializerType = deserializableType.ValidAttributes.First();

			var typeSymbol = context.SemanticModel.GetDeclaredSymbol( baseDeclaration );
			if( typeSymbol == null ) {
				return;
			}

			// Check if the type isn't deserializable, but inherits from a type with the ReflectionSerializer attibute
			if( !DeserializableAnalyzerHelper.IsDeserializable( typeSymbol, deserializableType ) ) {
				if( InheritsFromReflectionSerializer( context, typeSymbol, deserializableType ) ) {
					context.ReportDiagnostic( Diagnostic.Create( Diagnostics.ReflectionSerializerDescendantsMustBeDeserializable, baseDeclaration.GetLocation(), typeSymbol.Name ) );
				}
				return;
			}

			var inAllowedList = DeserializableAnalyzerHelper.GetAllowListFunction( additionalTexts, context.Compilation );

			var deserializableTypeArguments = typeSymbol.TypeArguments.Where( t => DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( t, deserializableType ) );

			// skip checking properties on interfaces because implementations may be able to mark them safe
			if( baseDeclaration is InterfaceDeclarationSyntax ) {
				return;
			}
			// Iterate through properties and fields to check for non-primitive types
			foreach( var member in baseDeclaration.Members ) {
				if( member is PropertyDeclarationSyntax propertyDeclaration ) {
					var typeSymbol = context.SemanticModel.GetTypeInfo( propertyDeclaration.Type ).Type;
					if( typeSymbol == null ) {
						continue;
					}

					if( deserializableTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
						continue;
					}

					if( baseDeclaration is RecordDeclarationSyntax ) {
						Console.Write( "" );
					}

					var arrowExpression = propertyDeclaration.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault();
					var expression = arrowExpression?.Expression;

					var returnStatement = propertyDeclaration.DescendantNodes().OfType<ReturnStatementSyntax>().FirstOrDefault();
					expression = expression ?? returnStatement?.Expression;
					if( expression != null ) {
						var typeExpression = expression;
						if( expression is TypeOfExpressionSyntax typeOfExpression ) {
							typeExpression = typeOfExpression.Type;
						}
						var type = context.SemanticModel.GetTypeInfo( typeExpression ).Type;

						if( type != null && deserializableTypeArguments.Any( t => t.Equals( type, SymbolEqualityComparer.Default ) ) ) {
							continue;
						}
					}

					AnalyzeTypeSymbol( context, deserializableType, inAllowedList, typeSymbol, propertyDeclaration.GetLocation(), propertyDeclaration );
				}
			}

			if( baseDeclaration is RecordDeclarationSyntax recordDeclarationSyntax ) {
				if( recordDeclarationSyntax.ParameterList == null ) {
					return;
				}
				foreach( var parameter in recordDeclarationSyntax.ParameterList.Parameters ) {
					if( parameter.Type == null ) {
						continue;
					}
					var typeSymbol = context.SemanticModel.GetTypeInfo( parameter.Type ).Type;
					if( typeSymbol == null ) {
						continue;
					}

					if( deserializableTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
						continue;
					}

					AnalyzeTypeSymbol( context, deserializableType, inAllowedList, typeSymbol, parameter.GetLocation() );
				}
			}
		}

		private static void AnalyzeTypeSymbol(
			SyntaxNodeAnalysisContext context,
			DeserializableTypeInfo deserializableType,
			Func<ITypeSymbol, bool> inAllowedList,
			ITypeSymbol typeSymbol,
			Location location,
			PropertyDeclarationSyntax? propertyDeclarationSyntax = null
			) {

			if(DeserializableAnalyzerHelper.IsDeserializableAtAllLevels( typeSymbol, inAllowedList, deserializableType ) ) {
				return;
			}

			// this check is expensive, but should be extremely rare
			if( propertyDeclarationSyntax is not null ) {
				var propertySymbol = context.SemanticModel.GetDeclaredSymbol( propertyDeclarationSyntax );
				if( propertySymbol is not null && propertySymbol.IsReadOnly ) {
					if( AnalyzeConstructorAssignmentsToReadonlyProperty( context, propertySymbol, deserializableType ) ) {
						return;
					}
				}
			}

			context.ReportDiagnostic( Diagnostic.Create( Diagnostics.ReflectionSerializerDescendantsMustBeDeserializable, location, typeSymbol.Name ) );
		}

		private static bool AnalyzeConstructorAssignmentsToReadonlyProperty(
			SyntaxNodeAnalysisContext context,
			IPropertySymbol propertySymbol,
			DeserializableTypeInfo deserializableType ) {
			INamedTypeSymbol containingType = propertySymbol.ContainingType;
			bool allConstructorsSafe = containingType.Constructors.Any();
			foreach( IMethodSymbol constructor in containingType.Constructors ) {
				bool currentSafe = false;
				// Analyze the constructor's body
				foreach( SyntaxNode node in constructor.DeclaringSyntaxReferences.SelectMany( syntaxRef => syntaxRef.GetSyntax().DescendantNodes() ) ) {
					// Find assignment expressions

					if( node is AssignmentExpressionSyntax assignment &&
						assignment.Left is IdentifierNameSyntax leftIdentifier &&
						leftIdentifier.Identifier.ValueText == propertySymbol.Name ) {

						// Check if the right-hand side of the assignment is a parameter
						if( assignment.Right is IdentifierNameSyntax rightIdentifier &&
							constructor.Parameters.Any( p => p.Name == rightIdentifier.Identifier.ValueText ) ) {
							IParameterSymbol parameterSymbol = constructor.Parameters.First( p => p.Name == rightIdentifier.Identifier.ValueText );
							var hasMustBeDeserializable = DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( parameterSymbol, deserializableType );
							if( DeserializableAnalyzerHelper.IsDeserializable( parameterSymbol.Type, deserializableType )
								|| hasMustBeDeserializable ) {
								currentSafe = true;
							}
						}
					}
				}
				if( !currentSafe ) {
					allConstructorsSafe = false;
				}
			}
			return allConstructorsSafe;
		}

		private static bool InheritsFromReflectionSerializer( SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol, DeserializableTypeInfo deserializableType ) {
			List<INamedTypeSymbol> symbolsInheritedFrom = new List<INamedTypeSymbol>( classSymbol.AllInterfaces );
			INamedTypeSymbol? currentType = classSymbol.BaseType;
			if( currentType != null ) {
				symbolsInheritedFrom.Add( currentType );
			}

			foreach( var symbol in symbolsInheritedFrom ) {
				if( symbol.SpecialType != SpecialType.System_Object && DeserializableAnalyzerHelper.IsDeserializable( symbol, deserializableType ) ) {
					return true;
				}
			}

			return false;
		}
	}
}

