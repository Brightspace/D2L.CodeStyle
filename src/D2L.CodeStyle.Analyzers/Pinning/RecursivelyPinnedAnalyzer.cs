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

			var pinningType = new MustBePinnedType( mustBePinnedSymbol, true, Diagnostics.MustBeDeserializableRequiresRecursivelyPinned, Diagnostics.ArgumentShouldBeDeserializable, null );

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

					AnalyzeTypeSymbol( context, pinnedAttributeSymbol, inAllowedList, typeSymbol, propertyDeclaration.GetLocation() );
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

					AnalyzeTypeSymbol( context, pinnedAttributeSymbol, inAllowedList, typeSymbol, parameter.GetLocation() );
				}
			}
		}

		private static void AnalyzeTypeSymbol(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol pinnedAttributeSymbol,
			Func<ITypeSymbol, bool> inAllowedList,
			ITypeSymbol typeSymbol,
			Location location ) {

			if( PinnedAnalyzerHelper.IsExemptFromPinning( typeSymbol, inAllowedList, out ITypeSymbol actualType)) {
				if( typeSymbol is INamedTypeSymbol namedTypeSymbol ) {
					foreach(ITypeSymbol childType in namedTypeSymbol.TypeArguments) {
						AnalyzeTypeSymbol(
							context,
							pinnedAttributeSymbol,
							inAllowedList,
							childType,
							location );
					}
				}
				return;
			}

			if( !PinnedAnalyzerHelper.IsRecursivelyPinned( actualType, pinnedAttributeSymbol ) ) {
				context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RecursivePinnedDescendantsMustBeRecursivelyPinned, location, typeSymbol.Name ) );
			}
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

