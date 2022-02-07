using System.Collections.Immutable;
using System.Text;
using D2L.CodeStyle.SpecTests.Parser;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Generators.TestFixtures {

	internal static class TestFixtureRenderer {

		public sealed record class Args(
			string Namespace,
			ImmutableArray<string> ContainerClassNames,
			string FixtureClassName,
			AnalyzerSpec Spec,
			string SpecName,
			string SpecSource
		);

		public static string Render( Args args ) {

			StringBuilder buffer = new( args.SpecSource.Length * 2 );

			using( StringWriter stringWriter = new( buffer ) )
			using( CSharpTextWriter writer = new( stringWriter ) ) {

				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using D2L.CodeStyle.SpecTests._Generated_;" );
				writer.WriteLine( "using D2L.CodeStyle.SpecTests.Framework;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis.Diagnostics;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis.Text;" );
				writer.WriteLine( "using NUnit.Framework;" );
				writer.WriteLine();

				writer.Write( "namespace " );
				writer.Write( args.Namespace );
				writer.WriteLine( " {" );
				writer.IndentBlock( () => {

					for( int i = 0; i < args.ContainerClassNames.Length - 1; i++ ) {
						writer.Write( "public partial class " );
						writer.Write( args.ContainerClassNames[ i ] );
						writer.WriteLine( " {" );
						writer.Indent++;
					}

					WriteTestFixtureClass( args, writer );

					for( int i = 0; i < args.ContainerClassNames.Length - 1; i++ ) {
						writer.Indent--;
						writer.WriteLine( '}' );
					}

				} );
				writer.WriteLine( '}' );
			}

			return buffer.ToString();
		}

		private static void WriteTestFixtureClass( Args args, CSharpTextWriter writer ) {

			writer.WriteLine();
			writer.WriteLine( "[TestFixture( Category = \"Spec\" )]" );
			writer.Write( "public partial class " );
			writer.Write( args.FixtureClassName );
			writer.WriteLine( " {" );
			writer.IndentBlock( () => {

				writer.WriteEmptyLine();
				writer.WriteLine( "private DiagnosticsComparison m_comparison;" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[OneTimeSetUp]" );
				writer.WriteLine( "public async Task OneTimeSetUp() {" );
				writer.IndentBlock( () => {

					writer.WriteEmptyLine();
					writer.Write( "DiagnosticAnalyzer analyzer = new global::" );
					writer.Write( args.Spec.AnalyzerQualifiedTypeName );
					writer.WriteLine( "();" );

					writer.WriteEmptyLine();
					writer.WriteLine( "ImmutableArray<Diagnostic> actualDiagnostics = await AnalyzerDiagnosticsProvider" );
					writer.IndentBlock( () => {

						writer.WriteLine( ".GetAnalyzerDiagnosticsAsync(" );
						writer.IndentBlock( () => {

							writer.WriteLine( "analyzer: analyzer," );
							writer.WriteLine( "additionalFiles: GlobalAdditionalFiles.AdditionalFiles," );

							writer.Write( "debugName: " );
							writer.WriteString( args.SpecName );
							writer.WriteLine( "," );

							writer.WriteLine( "metadataReferences: GlobalConfig.MetadataReferences," );
							writer.WriteLine( "source: Source" );

						} );
						writer.WriteLine( ");" );

					} );

					writer.WriteEmptyLine();
					writer.WriteLine( "m_comparison = AnalyzerDiagnosticsComparer.Compare(" );
					writer.IndentBlock( () => {
						writer.WriteLine( "GlobalConfig.DiagnosticDescriptors," );
						writer.WriteLine( "actualDiagnostics," );
						writer.WriteLine( "GetExpectedDiagnostics()" );
					} );
					writer.WriteLine( ");" );

				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[Test]" );
				writer.WriteLine( "public void ExpectedDiagnostics() {" );
				writer.IndentBlock( () => {
					writer.WriteLine( "Assert.Multiple( () => {" );
					writer.IndentBlock( () => {
						writer.WriteLine( "foreach( ComputedDiagnostic diagnostic in m_comparison.Missing ) {" );
						writer.IndentBlock( () => {
							writer.WriteLine( "Assert.Fail( \"An expected diagnostic was not reported: {0}\", diagnostic );" );
						} );
						writer.WriteLine( "}" );
					} );
					writer.WriteLine( "} );" );
				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				writer.WriteLine( "[Test]" );
				writer.WriteLine( "public void NoUnexpectedDiagnostics() {" );
				writer.IndentBlock( () => {
					writer.WriteLine( "Assert.Multiple( () => {" );
					writer.IndentBlock( () => {
						writer.WriteLine( "foreach( ComputedDiagnostic diagnostic in m_comparison.Unexpected ) {" );
						writer.IndentBlock( () => {
							writer.WriteLine( "Assert.Fail( \"An unexpected diagnostic was reported: {0}\", diagnostic );" );
						} );
						writer.WriteLine( "}" );
					} );
					writer.WriteLine( "} );" );
				} );
				writer.WriteLine( "}" );

				writer.WriteEmptyLine();
				WriteExpectedDiagnostics( args.Spec.ExpectedDiagnostics, writer );

				writer.WriteEmptyLine();
				WriteSourceConstant( args.SpecSource, writer );
				writer.WriteEmptyLine();
			} );
			writer.WriteLine( '}' );
		}

		private static void WriteExpectedDiagnostics(
				ImmutableArray<AnalyzerSpec.ExpectedDiagnostic> expectedDiagnostics,
				CSharpTextWriter writer
			) {

			writer.WriteLine( "#region ExpectedDiagnostics" );
			writer.WriteEmptyLine();
			writer.WriteLine( "private static IEnumerable<ExpectedDiagnostic> GetExpectedDiagnostics() {" );
			writer.IndentBlock( () => {

				if( expectedDiagnostics.IsEmpty ) {
					writer.WriteLine( "yield break;" );
					return;
				}

				foreach( AnalyzerSpec.ExpectedDiagnostic diagnostic in expectedDiagnostics ) {

					writer.WriteLine( "yield return new ExpectedDiagnostic(" );
					writer.IndentBlock( () => {

						writer.Write( "Alias: " );
						writer.WriteString( diagnostic.Alias );
						writer.WriteLine( "," );

						writer.Write( "LinePosition: " );
						WriteLinePositionSpan( diagnostic.Location.GetLineSpan().Span, writer );
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
