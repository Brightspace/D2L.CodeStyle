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
			Diagnostics.ReflectionSerializerDescendantsMustBeDeserializable,
			Diagnostics.ArgumentShouldBeDeserializable
		);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			DeserializableTypeInfo? deserializableType = DeserializableAnalyzerHelper.GetDeserializableTypeInfo( context.Compilation );
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
			TypeDeclarationSyntax? baseDeclaration = context.Node as TypeDeclarationSyntax;
			if( baseDeclaration == null ) {
				return;
			}

			INamedTypeSymbol reflectionSerializerType = deserializableType.ValidAttributes.First();

			INamedTypeSymbol? baseTypeSymbol = context.SemanticModel.GetDeclaredSymbol( baseDeclaration );
			if( baseTypeSymbol == null ) {
				return;
			}

			// Check if the type has the ReflectionSerializerAttribute, but inherits from a type with the ReflectionSerializer attibute
			if( !DeserializableAnalyzerHelper.HasReflectionSerializerAttribe( baseTypeSymbol, deserializableType ) ) {
				if( InheritsFromReflectionSerializer( context, baseTypeSymbol, deserializableType ) ) {
					context.ReportDiagnostic( Diagnostic.Create( Diagnostics.ReflectionSerializerDescendantsMustBeDeserializable, baseDeclaration.GetLocation(), baseTypeSymbol.Name ) );
				}
				return;
			}

			Func<ITypeSymbol, bool> inAllowedList = DeserializableAnalyzerHelper.GetAllowListFunction( additionalTexts, context.Compilation );

			IEnumerable<ITypeSymbol> deserializableTypeArguments = baseTypeSymbol.TypeArguments.Where( t => DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( t, deserializableType ) );

			// skip checking properties on interfaces because implementations may be able to mark them safe
			if( baseDeclaration is InterfaceDeclarationSyntax ) {
				return;
			}
			// Iterate through properties and fields to check for non-primitive types
			foreach( MemberDeclarationSyntax member in baseDeclaration.Members ) {
				if( member is PropertyDeclarationSyntax propertyDeclaration ) {
					ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo( propertyDeclaration.Type ).Type;
					if( typeSymbol == null ) {
						continue;
					}

					if( deserializableTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
						continue;
					}

					ArrowExpressionClauseSyntax? arrowExpression = propertyDeclaration.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault();
					ExpressionSyntax? expression = arrowExpression?.Expression;

					ReturnStatementSyntax? returnStatement = propertyDeclaration.DescendantNodes().OfType<ReturnStatementSyntax>().FirstOrDefault();
					expression = expression ?? returnStatement?.Expression;
					if( expression != null ) {
						ExpressionSyntax typeExpression = expression;
						if( expression is TypeOfExpressionSyntax typeOfExpression ) {
							typeExpression = typeOfExpression.Type;
						}
						ITypeSymbol? type = context.SemanticModel.GetTypeInfo( typeExpression ).Type;

						if( type != null && deserializableTypeArguments.Any( t => t.Equals( type, SymbolEqualityComparer.Default ) ) ) {
							continue;
						}
					}

					AnalyzeTypeSymbol( context, deserializableType, inAllowedList, typeSymbol, baseTypeSymbol, propertyDeclaration.GetLocation(), propertyDeclaration );
				}
			}

			if( baseDeclaration is RecordDeclarationSyntax recordDeclarationSyntax ) {
				if( recordDeclarationSyntax.ParameterList == null ) {
					return;
				}
				foreach( ParameterSyntax parameter in recordDeclarationSyntax.ParameterList.Parameters ) {
					if( parameter.Type == null ) {
						continue;
					}
					ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo( parameter.Type ).Type;
					if( typeSymbol == null ) {
						continue;
					}

					if( deserializableTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
						continue;
					}

					AnalyzeTypeSymbol( context, deserializableType, inAllowedList, typeSymbol, baseTypeSymbol, parameter.GetLocation() );
				}
			}
		}

		private static void AnalyzeTypeSymbol(
			SyntaxNodeAnalysisContext context,
			DeserializableTypeInfo deserializableType,
			Func<ITypeSymbol, bool> inAllowedList,
			ITypeSymbol typeSymbol,
			INamedTypeSymbol baseTypeSymbol,
			Location location,
			PropertyDeclarationSyntax? propertyDeclarationSyntax = null
			) {

			Queue<ITypeSymbol> typeQueue = new Queue<ITypeSymbol>();
			typeQueue.Enqueue( typeSymbol );

			bool unValidatedArgument = false;

			while( typeQueue.Count > 0 ) {
				ITypeSymbol currentType = typeQueue.Dequeue();

				if( !DeserializableAnalyzerHelper.IsExemptFromNeedingSerializationAttributes( currentType, inAllowedList, out ITypeSymbol actualType )
					&& !DeserializableAnalyzerHelper.IsDeserializable( actualType, deserializableType ) ) {
					// if generic type from base type then create a diagnostic for the generic type
					ITypeSymbol? matchingBaseTypeArgument = baseTypeSymbol.TypeArguments.FirstOrDefault( t => t.Equals( currentType, SymbolEqualityComparer.Default ));
					if( matchingBaseTypeArgument != null ) {
						context.ReportDiagnostic( Diagnostic.Create( Diagnostics.ArgumentShouldBeDeserializable, matchingBaseTypeArgument.Locations.FirstOrDefault(), currentType.Name ) );
					} else {
						unValidatedArgument = true;
					}

				}

				if( currentType is INamedTypeSymbol namedTypeSymbol ) {
					foreach( ITypeSymbol childType in namedTypeSymbol.TypeArguments ) {
						typeQueue.Enqueue( childType );
					}
				}
			}

			if( !unValidatedArgument ) {
				return;
			}

			// This check should be extremely rare and only exists to support legacy patterns. It's only when dealing with a readonly property that's not generic and not directly deserializable. (e.g. System.object)
			if( propertyDeclarationSyntax is not null ) {
				IPropertySymbol? propertySymbol = context.SemanticModel.GetDeclaredSymbol( propertyDeclarationSyntax );
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
				foreach( SyntaxNode node in constructor.DeclaringSyntaxReferences
					.SelectMany( syntaxRef => syntaxRef.GetSyntax().DescendantNodes()
					.Where(n => n is AssignmentExpressionSyntax) ) ) {
					// Find assignment expressions

					if( node is AssignmentExpressionSyntax assignment &&
						assignment.Left is IdentifierNameSyntax leftIdentifier &&
						leftIdentifier.Identifier.ValueText == propertySymbol.Name ) {

						// Check if the right-hand side of the assignment is a parameter
						if( assignment.Right is IdentifierNameSyntax rightIdentifier &&
							constructor.Parameters.Any( p => p.Name == rightIdentifier.Identifier.ValueText ) ) {
							IParameterSymbol parameterSymbol = constructor.Parameters.First( p => p.Name == rightIdentifier.Identifier.ValueText );
							bool hasMustBeDeserializable = DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( parameterSymbol, deserializableType );
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

			foreach( INamedTypeSymbol? symbol in symbolsInheritedFrom ) {
				if( symbol.SpecialType != SpecialType.System_Object && DeserializableAnalyzerHelper.HasReflectionSerializerAttribe( symbol, deserializableType ) ) {
					return true;
				}
			}

			return false;
		}
	}
}

