using System.Collections.Immutable;
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

			StringWriter buffer = new StringWriter();

			using( CSharpTextWriter writer = new CSharpTextWriter( buffer ) ) {

				writer.WriteLine( "using System;" );
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
				writer.Write( "private readonly DiagnosticAnalyzer m_analyzer = new global::" );
				writer.Write( spec.AnalyzerQualifiedTypeName );
				writer.WriteLine( "();" );

				writer.WriteEmptyLine();
				writer.WriteLine( "private readonly ImmutableArray<DiagnosticExpectation> m_expectedDiagnostics = ImmutableArray.Create(" );
				writer.IndentBlock( () => {

					int expectationCount = spec.ExpectedDiagnostics.Length;
					for( int i = 0; i < expectationCount; i++ ) {
						AnalyzerSpec.ExpectedDiagnostic expectation = spec.ExpectedDiagnostics[ i ];

						writer.WriteLine( "new ExpectedDiagnostic(" );
						writer.IndentBlock( () => {

							writer.Write( "Name: \"" );
							writer.WriteEscapedString( expectation.Name );
							writer.WriteLine( "\"," );

							writer.Write( "Location: " );
							WriteLocation( expectation.Location, writer );
							writer.WriteLine( "," );

							writer.Write( "MessageArguments: " );
							writer.WriteLine( "ImmutableArray<string>.Empty" );

						} );
						writer.Write( ")" );

						if( i < expectationCount - 1 ) {
							writer.Write( "," );
						}

						writer.WriteLine();
					}
				} );
				writer.WriteLine( ");" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[OneTimeSetUp]" );
				writer.WriteLine( "public void OneTimeSetUp() {" );
				writer.IndentBlock( () => {

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
				WriteSourceConstant( spec.Source, writer );
				writer.WriteEmptyLine();
			} );
			writer.WriteLine( '}' );
		}

		private static void WriteLocation( Location location, CSharpTextWriter writer ) {

			writer.WriteLine( "Locatation.Create(" );
			writer.IndentBlock( () => {

				writer.WriteLine( "filePath: \"\"," );

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

		private static void WriteSourceConstant(
				string source,
				CSharpTextWriter writer
			) {

			writer.WriteLine( "#region Source" );
			writer.WriteEmptyLine();
			writer.Write( "private const string Source = @\"" );

			for( int i = 0; i < source.Length; i++ ) {

				char character = source[ i ];
				if( character == '"' ) {
					writer.Write( '"' );
				}

				writer.Write( character );
			}

			writer.WriteLine( "\";" );
			writer.WriteEmptyLine();
			writer.WriteLine( "#endregion" );
		}
	}
}
