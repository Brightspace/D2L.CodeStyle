using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzerTypeArgumentsSniffer : DiagnosticAnalyzer {

		private const string InstancePath = @"C:\D2L\instances\lms\";
		private const string ReportOutputPath = @"C:\D2L\tmp\ImmutabilityAnalyzerTypeArgumentsSniffer";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.TypeArgumentLengthMismatch
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private readonly record struct SimpleNameTuple( SimpleNameSyntax Name, SymbolKind SymbolKind ) {
			public Location Location => Name.GetLocation();
			public SyntaxTree SyntaxTree => Name.SyntaxTree;
		};

		public void CompilationStart(
			CompilationStartAnalysisContext context
		) {

			ConcurrentBag<SimpleNameTuple> identifierNames = new();
			ConcurrentBag<SimpleNameTuple> genericNames = new();

			context.RegisterSyntaxNodeAction(
				ctx => {
					IdentifierNameSyntax identifierName = (IdentifierNameSyntax)ctx.Node;
					bool anazlye = ShouldAnalyzeTypeArguments( ctx, identifierName, out SymbolKind symbolKind );
					if( anazlye ) {
						identifierNames.Add( new( identifierName, symbolKind ) );
					}
				},
				SyntaxKind.IdentifierName
			);

			context.RegisterSyntaxNodeAction(
				ctx => {
					GenericNameSyntax genericName = (GenericNameSyntax)ctx.Node;
					bool anazlye = ShouldAnalyzeTypeArguments( ctx, genericName, out SymbolKind symbolKind );
					if( anazlye ) {
						genericNames.Add( new( genericName, symbolKind ) );
					}
				},
				SyntaxKind.GenericName
			);

			context.RegisterCompilationEndAction(
				ctx => {
					Compilation compilation = ctx.Compilation;

					WriteReports( identifierNames, ".identifierNames.txt" );
					WriteReports( genericNames, ".genericNames.txt" );
				}
			);
		}

		private static void WriteReports(
				IEnumerable<SimpleNameTuple> names,
				string extension
			) {

			IEnumerable<IGrouping<SyntaxTree, SimpleNameTuple>> namesBySyntaxTree = names
				.GroupBy( name => name.SyntaxTree );

			foreach( IGrouping<SyntaxTree, SimpleNameTuple> namesInSyntaxTree in namesBySyntaxTree ) {
				SyntaxTree syntaxTree = namesInSyntaxTree.Key;
				string instanceRelativePath = GetInstanceRelativePath( syntaxTree );

				string outputPath = Path.Combine(
						ReportOutputPath,
						Path.ChangeExtension( instanceRelativePath, extension )
					);

				string outputDirectory = Path.GetDirectoryName( outputPath );
				Directory.CreateDirectory( outputDirectory );

				using StreamWriter sw = new StreamWriter( outputPath, append: false, Encoding.UTF8 );
				foreach( SimpleNameTuple tuple in namesInSyntaxTree.OrderBy( n => n.Location.SourceSpan ) ) {

					sw.Write( tuple.Name );
					sw.Write( ", " );
					sw.Write( tuple.SymbolKind );
					sw.Write( ", " );
					sw.WriteLine( tuple.Location.SourceSpan );
				}
			}
		}

		private static string GetInstanceRelativePath( SyntaxTree syntaxTree ) {

			string filePath = syntaxTree.FilePath;
			if( !filePath.StartsWith( InstancePath, StringComparison.OrdinalIgnoreCase ) ) {
				throw new Exception( $"Unxpected syntax tree file path: { syntaxTree.FilePath } " );
			}

			return syntaxTree.FilePath.Substring( InstancePath.Length );
		}

		private readonly record struct SimpleNameSyntaxInfo(
				string Name,
				SymbolKind SymbolKind,
				Location Location
			);

		private static bool ShouldAnalyzeTypeArguments(
			SyntaxNodeAnalysisContext ctx,
			SimpleNameSyntax syntax,
			out SymbolKind symbolKind
		) {
			if( syntax.IsFromDocComment() ) {
				// ignore things in doccomments such as crefs
				symbolKind = default;
				return false;
			}

			SymbolInfo info = ctx.SemanticModel.GetSymbolInfo( syntax, ctx.CancellationToken );

			ISymbol? symbol = info.Symbol;
			if( symbol == null ) {
				symbolKind = default;
				return false;
			}

			// Ignore anything that cannot have type arguments/parameters
			if( !GetTypeParamsAndArgs( symbol, out var typeParameters, out var typeArguments ) ) {
				symbolKind = default;
				return false;
			}

			if( typeParameters.IsEmpty && typeArguments.IsEmpty ) {
				symbolKind = default;
				return false;
			}

			if( typeParameters.Length != typeArguments.Length ) {

				ctx.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.TypeArgumentLengthMismatch,
					syntax.GetLocation()
				) );

				symbolKind = default;
				return false;
			}

			symbolKind = symbol.Kind;
			return true;
		}

		private static bool GetTypeParamsAndArgs( ISymbol type, out ImmutableArray<ITypeParameterSymbol> typeParameters, out ImmutableArray<ITypeSymbol> typeArguments ) {
			switch( type ) {
				case IMethodSymbol method:
					typeParameters = method.TypeParameters;
					typeArguments = method.TypeArguments;
					return true;
				case INamedTypeSymbol namedType:
					typeParameters = namedType.TypeParameters;
					typeArguments = namedType.TypeArguments;
					return true;
				default:
					return false;
			}
		}
	}
}
