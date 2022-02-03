using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class CategoryAnalyzer : DiagnosticAnalyzer {

		private static readonly ImmutableSortedSet<string> RequiredCategories = ImmutableSortedSet
			.Create(
				StringComparer.OrdinalIgnoreCase,
				"Unit",
				"Integration",
				"System",
				"Load",
				"UI"
			);

		private static readonly ImmutableSortedSet<string> ProhibitedAssemblyCategories = ImmutableSortedSet
			.Create(
				StringComparer.OrdinalIgnoreCase,
				"Isolated"
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.NUnitCategory
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private static void OnCompilationStart( CompilationStartAnalysisContext context ) {
			if( !TryLoadNUnitTypes( context.Compilation, out NUnitTypes types ) ) {
				return;
			}

			ImmutableHashSet<string> bannedCategories = LoadBannedCategoriesList( context.Options );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeMethod(
					context: ctx,
					bannedCategories: bannedCategories,
					types: types,
					syntax: ctx.Node as MethodDeclarationSyntax
				),
				SyntaxKind.MethodDeclaration
			);

			context.RegisterCompilationEndAction(
				ctx => OnCompilationEnd( ctx, types )
			);
		}

		private static void OnCompilationEnd( CompilationAnalysisContext context, NUnitTypes types ) {
			VisitCategories( types, context.Compilation.Assembly, ( category, attribute ) => {
				if( !ProhibitedAssemblyCategories.Contains( category ) ) {
					return;
				}

				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.NUnitCategory,
					attribute.ApplicationSyntaxReference.GetSyntax( context.CancellationToken ).GetLocation(),
					$"Assemblies cannot be categorized as any of [{string.Join( ", ", ProhibitedAssemblyCategories )}], but saw '{category}'."
				) );
			} );
		}

		private static void AnalyzeMethod(
			SyntaxNodeAnalysisContext context,
			ImmutableHashSet<string> bannedCategories,
			NUnitTypes types,
			MethodDeclarationSyntax syntax
		) {
			SemanticModel model = context.SemanticModel;

			IMethodSymbol method = model.GetDeclaredSymbol( syntax, context.CancellationToken );
			if( method == null ) {
				return;
			}

			if( !IsTestMethod( types, method ) ) {
				return;
			}

			ImmutableSortedSet<string> categories = GatherTestMethodCategories( types, method );
			if( !categories.Overlaps( RequiredCategories ) ) {
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.NUnitCategory,
					syntax.Identifier.GetLocation(),
					$"Test must be categorized as one of [{string.Join( ", ", RequiredCategories )}], but saw [{string.Join( ", ", categories )}]. See http://docs.dev.d2l/index.php/Test_Categories."
				) );
			}

			if( categories.Overlaps( bannedCategories ) ) {
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.NUnitCategory,
					syntax.Identifier.GetLocation(),
					$"Test is categorized as [{string.Join( ", ", categories )}]; however, the following categories are banned in this assembly: [{string.Join( ", ", bannedCategories )}]."
				) );
			}
		}

		private static bool IsTestMethod(
			NUnitTypes types,
			IMethodSymbol method
		) {
			foreach( AttributeData attribute in method.GetAttributes() ) {
				INamedTypeSymbol attributeType = attribute.AttributeClass;
				if( types.TestAttributes.Contains( attributeType ) ) {
					return true;
				}
			}

			return false;
		}

		private static ImmutableSortedSet<string> GatherTestMethodCategories(
			NUnitTypes types,
			IMethodSymbol method
		) {
			ImmutableSortedSet<string> methodCategories = GetCategories( types, method );
			ImmutableSortedSet<string> fixtureCategories = GetCategories( types, method.ContainingType );
			ImmutableSortedSet<string> assemblyCategories = GetCategories( types, method.ContainingAssembly );

			ImmutableSortedSet<string> categories =
				methodCategories
					.Union( fixtureCategories )
					.Union( assemblyCategories );

			return categories;
		}

		private static ImmutableSortedSet<string> GetCategories(
			NUnitTypes types,
			ISymbol symbol
		) {
			var categories = ImmutableSortedSet.CreateBuilder<string>( StringComparer.OrdinalIgnoreCase );

			VisitCategories( types, symbol, ( category, _ ) => categories.Add( category ) );

			return categories.ToImmutable();
		}

		private static void VisitCategories(
			NUnitTypes types,
			ISymbol symbol,
			Action<string, AttributeData> visitor
		) {
			foreach( AttributeData attribute in symbol.GetAttributes() ) {
				INamedTypeSymbol attributeType = attribute.AttributeClass;
				if( types.CategoryAttribute.Equals( attributeType, SymbolEqualityComparer.Default ) ) {
					VisitCategoryAttribute( attribute, visitor );
					continue;
				}

				if( types.TestFixtureAttribute.Equals( attributeType, SymbolEqualityComparer.Default ) ) {
					VisitTestFixtureAttribute( attribute, visitor );
					continue;
				}
			}

			if( symbol is INamedTypeSymbol typeSymbol ) {
				if( typeSymbol.BaseType != null ) {
					VisitCategories( types, typeSymbol.BaseType, visitor );
				}
			}
		}

		private static void VisitCategoryAttribute(
			AttributeData attribute,
			Action<string, AttributeData> visitor
		) {
			ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;
			if( args.Length != 1 ) {
				return;
			}

			TypedConstant arg = args[0];

			if( arg.Type.SpecialType != SpecialType.System_String ) {
				return;
			}

			string category = arg.Value as string;
			visitor( category, attribute );
		}

		private static void VisitTestFixtureAttribute(
			AttributeData attribute,
			Action<string, AttributeData> visitor
		) {
			foreach( KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments ) {
				if( namedArg.Key != "Category" ) {
					continue;
				}

				TypedConstant arg = namedArg.Value;

				if( arg.Type.SpecialType != SpecialType.System_String ) {
					continue;
				}

				string categoryCsv = arg.Value as string;
				foreach( string category in categoryCsv.Split( ',' ) ) {
					visitor( category.Trim(), attribute );
				}
			}
		}

		private static bool TryLoadNUnitTypes(
			Compilation compilation,
			out NUnitTypes types
		) {
			INamedTypeSymbol categoryAttribute = compilation.GetTypeByMetadataName( "NUnit.Framework.CategoryAttribute" );
			if( categoryAttribute == null || categoryAttribute.TypeKind == TypeKind.Error ) {
				types = null;
				return false;
			}

			ImmutableHashSet<INamedTypeSymbol> testAttributes = ImmutableHashSet
				.Create<INamedTypeSymbol>(
					SymbolEqualityComparer.Default,
					compilation.GetTypeByMetadataName( "NUnit.Framework.TestAttribute" ),
					compilation.GetTypeByMetadataName( "NUnit.Framework.TestCaseAttribute" ),
					compilation.GetTypeByMetadataName( "NUnit.Framework.TestCaseSourceAttribute" ),
					compilation.GetTypeByMetadataName( "NUnit.Framework.TheoryAttribute" )
				);

			INamedTypeSymbol testFixtureAttribute = compilation.GetTypeByMetadataName( "NUnit.Framework.TestFixtureAttribute" );

			types = new NUnitTypes( categoryAttribute, testAttributes, testFixtureAttribute );
			return true;
		}

		private sealed class NUnitTypes {

			internal NUnitTypes(
				INamedTypeSymbol categoryAttribute,
				ImmutableHashSet<INamedTypeSymbol> testAttributes,
				INamedTypeSymbol testFixtureAttribute
			) {
				CategoryAttribute = categoryAttribute;
				TestAttributes = testAttributes;
				TestFixtureAttribute = testFixtureAttribute;
			}

			public INamedTypeSymbol CategoryAttribute { get; }
			public ImmutableHashSet<INamedTypeSymbol> TestAttributes { get; }
			public INamedTypeSymbol TestFixtureAttribute { get; }
		}

		private static ImmutableHashSet<string> LoadBannedCategoriesList(
			AnalyzerOptions options
		) {
			ImmutableHashSet<string>.Builder bannedList = ImmutableHashSet.CreateBuilder(
				StringComparer.Ordinal
			);

			AdditionalText bannedListFile = options.AdditionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == "BannedTestCategoriesList.txt"
			);

			if( bannedListFile == null ) {
				return bannedList.ToImmutableHashSet();
			}

			SourceText allowedListText = bannedListFile.GetText();

			foreach( TextLine line in allowedListText.Lines ) {
				bannedList.Add( line.ToString().Trim() );
			}

			return bannedList.ToImmutableHashSet();
		}
	}
}
