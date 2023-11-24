using System.Collections.Immutable;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Pinning {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class RecursivelyPinnedAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.RecursivePinnedDescendantsMustBeRecursivelyPinned
			);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSyntaxNodeAction( (ctx) => AnalyzeSyntaxNode(ctx, context.Options.AdditionalFiles),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.RecordDeclaration,
				SyntaxKind.StructDeclaration );
		}

		private static void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context, ImmutableArray<AdditionalText> additionalTexts ) {
			var baseDeclaration = context.Node as TypeDeclarationSyntax;
			if( baseDeclaration == null ) {
				return;
			}

			var pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol == null ) {
				return;
			}

			var mustBePinnedSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.MustBeDeserializableAttributeName );
			if( mustBePinnedSymbol == null ) {
				return;
			}

			var pinningType = PinnedAnalyzerHelper.GetMustBePinnedType( context.Compilation, true );

			if( pinningType == null ) {
				return;
			}

			var classSymbol = context.SemanticModel.GetDeclaredSymbol( baseDeclaration );
			if( classSymbol == null ) {
				return;
			}

			// Check if the class is marked with the "RecursivelyPinned" attribute
			if( !PinnedAnalyzerHelper.IsRecursivelyPinned( classSymbol, pinnedAttributeSymbol )) {
				if( InheritsFromRecursivelyPinned( context, classSymbol, pinnedAttributeSymbol ) ) {
					context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RecursivePinnedDescendantsMustBeRecursivelyPinned, baseDeclaration.GetLocation(), classSymbol.Name ) );
				}
				return;
			}

			var inAllowedList = PinnedAnalyzerHelper.AllowedUnpinnedTypes( additionalTexts, context.Compilation );

			var pinnedTypeArguments = classSymbol.TypeArguments.Where( t => PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( t, pinningType, out _ ) );

			// skip checking properties on interfaces because implementations may be able to mark them safe
			if(baseDeclaration is InterfaceDeclarationSyntax) {
				return;
			}
			// Iterate through properties and fields to check for non-primitive types
			foreach( var member in baseDeclaration.Members ) {
				if(member is PropertyDeclarationSyntax propertyDeclaration ) {
					var typeSymbol = context.SemanticModel.GetTypeInfo( propertyDeclaration.Type ).Type;
					if( typeSymbol == null ) {
						continue;
					}

					if( pinnedTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
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

						if( type != null && pinnedTypeArguments.Any( t => t.Equals( type, SymbolEqualityComparer.Default ))) {
							continue;
						}
					}

					AnalyzeTypeSymbol( context, pinnedAttributeSymbol, inAllowedList, typeSymbol, propertyDeclaration.GetLocation(), mustBePinnedSymbol, propertyDeclaration );
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

					if( pinnedTypeArguments.Any( t => t.Equals( typeSymbol, SymbolEqualityComparer.Default ) ) ) {
						continue;
					}

					AnalyzeTypeSymbol( context, pinnedAttributeSymbol, inAllowedList, typeSymbol, parameter.GetLocation(), mustBePinnedSymbol );
				}
			}
		}

		private static void AnalyzeTypeSymbol(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol pinnedAttributeSymbol,
			Func<ITypeSymbol, bool> inAllowedList,
			ITypeSymbol typeSymbol,
			Location location,
			INamedTypeSymbol mustBePinnedSymbol,
			PropertyDeclarationSyntax? propertyDeclarationSyntax = null
			) {

			if( PinnedAnalyzerHelper.IsExemptFromPinning( typeSymbol, inAllowedList, out ITypeSymbol actualType)) {
				if( typeSymbol is INamedTypeSymbol namedTypeSymbol ) {
					foreach(ITypeSymbol childType in namedTypeSymbol.TypeArguments) {
						AnalyzeTypeSymbol(
							context,
							pinnedAttributeSymbol,
							inAllowedList,
							childType,
							location,
							mustBePinnedSymbol);
					}
				}
				return;
			}

			if( PinnedAnalyzerHelper.IsRecursivelyPinned( actualType, pinnedAttributeSymbol ) ) {
				return;
			}

			// this check is expensive, but should be extremely rare
			if( propertyDeclarationSyntax is not null ) {
				var propertySymbol = context.SemanticModel.GetDeclaredSymbol( propertyDeclarationSyntax );
				if( propertySymbol is not null && propertySymbol.IsReadOnly ) {
					if( AnalyzeConstructorAssignmentsToReadonlyProperty( context, propertySymbol, pinnedAttributeSymbol, mustBePinnedSymbol ) ) {
						return;
					}
				}
			}

			context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RecursivePinnedDescendantsMustBeRecursivelyPinned, location, typeSymbol.Name ) );
		}

		private static bool AnalyzeConstructorAssignmentsToReadonlyProperty(
			SyntaxNodeAnalysisContext context,
			IPropertySymbol propertySymbol,
			INamedTypeSymbol pinnedAttributeSymbol,
			INamedTypeSymbol mustBePinnedSymbol) {
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
							var hasMustBeDeserializable = PinnedAnalyzerHelper.TryGetPinnedAttribute( parameterSymbol, mustBePinnedSymbol, out _ );
							if( PinnedAnalyzerHelper.IsRecursivelyPinned( parameterSymbol.Type, pinnedAttributeSymbol )
								|| hasMustBeDeserializable  ) {
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

		private static bool InheritsFromRecursivelyPinned( SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol, INamedTypeSymbol pinnedAttributeSymbol ) {
			List<INamedTypeSymbol> symbolsInheritedFrom = new List<INamedTypeSymbol>( classSymbol.AllInterfaces );
			INamedTypeSymbol? currentType = classSymbol.BaseType;
			if( currentType != null ) {
				symbolsInheritedFrom.Add( currentType );
			}

			foreach( var symbol in symbolsInheritedFrom ) {
				if( symbol.SpecialType != SpecialType.System_Object && PinnedAnalyzerHelper.IsRecursivelyPinned( symbol, pinnedAttributeSymbol ) ) {
					return true;
				}
			}

			return false;
		}
	}
}

