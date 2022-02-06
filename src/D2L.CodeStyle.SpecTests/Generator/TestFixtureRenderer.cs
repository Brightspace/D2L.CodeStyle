using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Generator {

	internal static class TestFixtureRenderer {

		public static string Render(
				string @namespace,
				ImmutableArray<string> containerClassNames,
				string fixtureClassName,
				AnalyzerSpec spec
			) {

			StringBuilder buffer = new( spec.Source.Length * 2 );

			using( StringWriter stringWriter = new( buffer ) )
			using( CSharpTextWriter writer = new( stringWriter ) ) {

				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using D2L.CodeStyle.SpecTests.Framework;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis.Diagnostics;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis.Text;" );
				writer.WriteLine( "using NUnit.Framework;" );
				writer.WriteLine();

				writer.Write( "namespace " );
				writer.Write( @namespace );
				writer.WriteLine( " {" );
				writer.IndentBlock( () => {

					for( int i = 0; i < containerClassNames.Length - 1; i++ ) {
						writer.Write( "public partial class " );
						writer.Write( containerClassNames[ i ] );
						writer.WriteLine( " {" );
						writer.Indent++;
					}

					WriteTestFixtureClass( fixtureClassName, spec, writer );

					for( int i = 0; i < containerClassNames.Length - 1; i++ ) {
						writer.Indent--;
						writer.WriteLine( '}' );
					}

				} );
				writer.Indent--;
				writer.WriteLine( '}' );
			}

			return buffer.ToString();
		}

		private static void WriteTestFixtureClass(
				string className,
				AnalyzerSpec spec,
				CSharpTextWriter writer
			) {

			writer.WriteLine();
			writer.WriteLine( "[TestFixture( Category = \"Spec\" )]" );
			writer.Write( "public partial class " );
			writer.Write( className );
			writer.WriteLine( " {" );
			writer.IndentBlock( () => {


				writer.WriteEmptyLine();
				writer.WriteLine( "[OneTimeSetUp]" );
				writer.WriteLine( "public async Task OneTimeSetUp() {" );
				writer.IndentBlock( () => {

					writer.WriteEmptyLine();
					writer.Write( "DiagnosticAnalyzer analyzer = new global::" );
					writer.Write( spec.AnalyzerQualifiedTypeName );
					writer.WriteLine( "();" );
					writer.WriteLine( "await SpecTestRunner.RunAsync( analyzer, Source );" );

				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[Test]" );
				writer.WriteLine( "public void ExpectedDiagnostics() {" );
				writer.IndentBlock( () => {

				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[Test]" );
				writer.WriteLine( "public void NoUnexpectedDiagnostics() {" );
				writer.IndentBlock( () => {

				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				WriteDiagnosticExpectations( spec.ExpectedDiagnostics, writer );

				writer.WriteEmptyLine();
				WriteSourceConstant( spec.Source, writer );
				writer.WriteEmptyLine();
			} );
			writer.WriteLine( '}' );
		}

		private static void WriteDiagnosticExpectations(
				ImmutableArray<AnalyzerSpec.ExpectedDiagnostic> expectedDiagnostics,
				CSharpTextWriter writer
			) {

			writer.WriteLine( "#region ExpectedDiagnostics" );
			writer.WriteEmptyLine();
			writer.WriteLine( "private static IEnumerable<DiagnosticExpectation> GetExpectedDiagnostics() {" );
			writer.IndentBlock( () => {

				if( expectedDiagnostics.IsEmpty ) {
					writer.WriteLine( "yield break;" );
					return;
				}

				foreach( AnalyzerSpec.ExpectedDiagnostic diagnostic in expectedDiagnostics ) {

					writer.WriteLine( "yield return new DiagnosticExpectation(" );
					writer.IndentBlock( () => {

						writer.Write( "Name: " );
						writer.WriteString( diagnostic.Name );
						writer.WriteLine( "," );

						writer.Write( "Location: " );
						WriteLocation( diagnostic.Location, writer );
						writer.WriteLine( "," );

						writer.Write( "MessageArguments: " );
						WriteImmutableArrayOfStrings( diagnostic.MessageArguments, writer );
						writer.WriteLine();

					} );
					writer.WriteLine( ");" );
				}
			} );

			writer.WriteLine( "}" );
			writer.WriteEmptyLine();
			writer.WriteLine( "#endregion" );
		}

		private static void WriteSourceConstant(
				string source,
				CSharpTextWriter writer
			) {

			writer.WriteLine( "#region Source" );
			writer.WriteEmptyLine();
			writer.Write( "private const string Source = " );
			writer.WriteMultiLineString( source );
			writer.WriteLine( ";" );
			writer.WriteEmptyLine();
			writer.WriteLine( "#endregion" );
		}

		private static void WriteImmutableArrayOfStrings( ImmutableArray<string> array, CSharpTextWriter writer ) {

			if( array.IsEmpty ) {
				writer.Write( "ImmutableArray<string>.Empty" );
				return;
			}

			writer.WriteLine( "ImmutableArray.Create(" );
			writer.IndentBlock( () => {

				for( int i = 0; i < array.Length; i++ ) {

					writer.WriteString( array[ i ] );

					if( i < array.Length - 1 ) {
						writer.Write( ',' );
					}

					writer.WriteEmptyLine();
				}
			} );
			writer.Write( ")" );
		}

		private static void WriteLocation( Location location, CSharpTextWriter writer ) {

			writer.WriteLine( "Location.Create(" );
			writer.IndentBlock( () => {

				writer.WriteLine( "filePath: string.Empty," );

				writer.Write( "textSpan: " );
				WriteTextSpan( location.SourceSpan, writer );
				writer.WriteLine( "," );

				writer.Write( "lineSpan: " );
				WriteLinePositionSpan( location.GetLineSpan().Span, writer );
				writer.WriteLine();
			} );
			writer.Write( ")" );
		}

		private static void WriteTextSpan( TextSpan textSpan, CSharpTextWriter writer ) {

			writer.Write( "new TextSpan( start: " );
			writer.Write( textSpan.Start );
			writer.Write( ", length: " );
			writer.Write( textSpan.Length );
			writer.Write( " )" );
		}

		private static void WriteLinePositionSpan( LinePositionSpan linePositionSpan, CSharpTextWriter writer ) {

			writer.WriteLine( "new LinePositionSpan(" );
			writer.IndentBlock( () => {

				writer.Write( "start: " );
				WriteLinePosition( linePositionSpan.Start, writer );
				writer.WriteLine( "," );

				writer.Write( "end: " );
				WriteLinePosition( linePositionSpan.End, writer );
				writer.WriteLine();
			} );
			writer.Write( ")" );
		}

		private static void WriteLinePosition( LinePosition linePosition, CSharpTextWriter writer ) {

			writer.Write( "new LinePosition( line: " );
			writer.Write( linePosition.Line );
			writer.Write( ", character: " );
			writer.Write( linePosition.Character );
			writer.Write( " )" );
		}
	}
}
