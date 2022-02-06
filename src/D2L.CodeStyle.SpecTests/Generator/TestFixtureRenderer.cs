using System.Collections.Immutable;

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
				writer.WriteLine( "using Microsoft.CodeAnalysis.Diagnostics;" );
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
