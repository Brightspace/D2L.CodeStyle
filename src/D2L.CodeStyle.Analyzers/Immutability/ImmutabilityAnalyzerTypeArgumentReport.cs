using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Immutability {

	internal static class ImmutabilityAnalyzerTypeArgumentReport {

		private const string InstancePath = @"C:\D2L\instances\lms\";
		private const string ReportOutputBasePath = @"C:\D2L\tmp\";

		public readonly record struct SimpleNameTuple( SimpleNameSyntax Name, SymbolKind SymbolKind ) {
			public Location Location => Name.GetLocation();
			public SyntaxTree SyntaxTree => Name.SyntaxTree;
		};

		public static void WriteReports(
				string reportName,
				IEnumerable<SimpleNameTuple> names
			) {

			IEnumerable<IGrouping<SyntaxTree, SimpleNameTuple>> namesBySyntaxTree = names
				.GroupBy( name => name.SyntaxTree );

			foreach( IGrouping<SyntaxTree, SimpleNameTuple> namesInSyntaxTree in namesBySyntaxTree ) {
				SyntaxTree syntaxTree = namesInSyntaxTree.Key;
				string instanceRelativePath = GetInstanceRelativePath( syntaxTree );

				string outputPath = Path.Combine(
						ReportOutputBasePath,
						reportName,
						$"{ instanceRelativePath }.csv"
					);

				string outputDirectory = Path.GetDirectoryName( outputPath );
				Directory.CreateDirectory( outputDirectory );

				using StreamWriter sw = new StreamWriter( outputPath, append: false, Encoding.UTF8 );
				foreach( SimpleNameTuple tuple in namesInSyntaxTree.OrderBy( n => n.Location.SourceSpan ) ) {

					FileLinePositionSpan linePositionSpan = tuple.Location.GetLineSpan();

					sw.Write( tuple.Name );
					sw.Write( "," );
					sw.Write( tuple.SymbolKind );
					sw.Write( "," );
					WriteLinePosition( sw, linePositionSpan.StartLinePosition );
					sw.Write( "," );
					WriteLinePosition( sw, linePositionSpan.EndLinePosition );
					sw.WriteLine();
				}
			}
		}

		private static void WriteLinePosition( StreamWriter sw, LinePosition linePosition ) {

			sw.Write( '"' );
			sw.Write( linePosition.Line );
			sw.Write( "," );
			sw.Write( linePosition.Character );
			sw.Write( '"' );
		}

		private static string GetInstanceRelativePath( SyntaxTree syntaxTree ) {

			string filePath = syntaxTree.FilePath;
			if( !filePath.StartsWith( InstancePath, StringComparison.OrdinalIgnoreCase ) ) {
				throw new Exception( $"Unxpected syntax tree file path: { syntaxTree.FilePath } " );
			}

			return syntaxTree.FilePath.Substring( InstancePath.Length );
		}
	}
}
