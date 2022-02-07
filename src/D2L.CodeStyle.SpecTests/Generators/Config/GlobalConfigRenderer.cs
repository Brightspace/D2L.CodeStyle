using System.Collections.Immutable;

namespace D2L.CodeStyle.SpecTests.Generators.Config {

	internal static class GlobalConfigRenderer {

		public static string Render( GlobalConfig config ) {

			using StringWriter buffer = new();
			using( CSharpTextWriter writer = new( buffer ) ) {

				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using System.Reflection;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using D2L.CodeStyle.SpecTests.Framework;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis;" );
				writer.WriteLine();

				writer.WriteLine( "namespace D2L.CodeStyle.SpecTests._Generated_ {" );
				writer.IndentBlock( () => {

					writer.WriteEmptyLine();
					writer.WriteLine( "internal static class GlobalConfig {" );
					writer.IndentBlock( () => {

						DiagnosticDescriptorCatalog( config.DiagnosticDescriptorSourceTypes, writer );
						RenderMetadataReferences( config.ReferenceAssemblies, writer ); ;

					} );
					writer.WriteLine( "}" );

				} );
				writer.WriteLine( "}" );
			}

			return buffer.ToString();
		}

		private static void DiagnosticDescriptorCatalog(
				ImmutableArray<string> diagnosticDescriptorSourceTypes,
				CSharpTextWriter writer
			) {

			writer.WriteEmptyLine();
			writer.WriteLine( "private static readonly Lazy<DiagnosticDescriptorCatalog> m_diagnosticDescriptorCatalog = new  Lazy<DiagnosticDescriptorCatalog>(" );
			writer.IndentBlock( () => {
				writer.WriteLine( "() => DiagnosticDescriptorCatalog.Create(" );
				writer.IndentBlock( () => {

					for( int i = 0; i < diagnosticDescriptorSourceTypes.Length; i++ ) {

						writer.Write( "Type.GetType( " );
						writer.WriteString( diagnosticDescriptorSourceTypes[ i ] );
						writer.Write( ", throwOnError: true )" );

						if( i < diagnosticDescriptorSourceTypes.Length - 1 ) {
							writer.Write( ',' );
						}

						writer.WriteLine();
					}

				} );
				writer.WriteLine( ")" );
			} );
			writer.WriteLine( ");" );

			writer.WriteEmptyLine();
			writer.WriteLine( "public static DiagnosticDescriptorCatalog DiagnosticDescriptors => m_diagnosticDescriptorCatalog.Value;" );
		}

		private static void RenderMetadataReferences(
				ImmutableArray<string> assemblyNames,
				CSharpTextWriter writer
			) {

			writer.WriteEmptyLine();
			writer.WriteLine( "private static readonly Lazy<ImmutableArray<MetadataReference>> m_metadataReferences = new  Lazy<ImmutableArray<MetadataReference>>(" );
			writer.IndentBlock( () => {
				writer.WriteLine( "() => GetMetadataReferences().ToImmutableArray()," );
				writer.WriteLine( "LazyThreadSafetyMode.ExecutionAndPublication" );
			} );
			writer.WriteLine( ");" );

			writer.WriteEmptyLine();
			writer.WriteLine( "public static ImmutableArray<MetadataReference> MetadataReferences => m_metadataReferences.Value;" );

			writer.WriteEmptyLine();
			writer.WriteLine( "private static IEnumerable<MetadataReference> GetMetadataReferences() {" );
			writer.IndentBlock( () => {

				foreach( string assemblyName in assemblyNames ) {
					writer.Write( "yield return GetMetadataReference( " );
					writer.WriteString( assemblyName );
					writer.WriteLine( " );" );
				}

			} );
			writer.WriteLine( "}" );

			writer.WriteEmptyLine();
			writer.WriteLine( "private static MetadataReference GetMetadataReference( string assemblyName ) {" );
			writer.IndentBlock( () => {
				writer.WriteLine( "Assembly assembly = Assembly.Load( assemblyName );" );
				writer.WriteLine( "return MetadataReference.CreateFromFile( assembly.Location );" );
			} );
			writer.WriteLine( "}" );
		}
	}
}
