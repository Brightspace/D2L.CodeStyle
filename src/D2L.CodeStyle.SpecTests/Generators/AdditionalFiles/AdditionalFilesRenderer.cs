using System.Collections.Immutable;

namespace D2L.CodeStyle.SpecTests.Generators.AdditionalFiles {

	internal static class AdditionalFilesRenderer {

		public static string Render( ImmutableArray<AdditionalFile> additionalFiles ) {

			using StringWriter stringWriter = new();
			using( CSharpTextWriter writer = new( stringWriter ) ) {

				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using D2L.CodeStyle.SpecTests.Framework;" );
				writer.WriteLine( "using Microsoft.CodeAnalysis;" );
				writer.WriteLine();

				writer.WriteLine( "namespace D2L.CodeStyle.SpecTests._Generated_ {" );
				writer.IndentBlock( () => {

					writer.WriteEmptyLine();
					writer.WriteLine( "internal static class GlobalAdditionalFiles {" );
					writer.IndentBlock( () => {

						writer.WriteEmptyLine();
						writer.WriteLine( "public static ImmutableArray<AdditionalText> AdditionalFiles { get; }" );

						writer.WriteEmptyLine();
						writer.WriteLine( "static GlobalAdditionalFiles() {" );
						writer.IndentBlock( () => {

							writer.WriteEmptyLine();
							writer.WriteLine( "ImmutableArray<AdditionalText>.Builder builder = ImmutableArray.CreateBuilder<AdditionalText>();" );

							foreach( AdditionalFile additionalFile in additionalFiles ) {

								string text = File.ReadAllText( additionalFile.OriginalPath );

								writer.WriteEmptyLine();
								writer.WriteLine( "builder.Add( new AdditionalTextFile(" );
								writer.IndentBlock( () => {

									writer.Write( "path: " );
									writer.WriteString( additionalFile.VirtualPath );
									writer.WriteLine( "," );

									writer.Write( "text: " );
									writer.WriteMultiLineString( text );
									writer.WriteLine();

								} );
								writer.WriteLine( ") );" );
							}

							writer.WriteEmptyLine();
							writer.WriteLine( "AdditionalFiles = builder.ToImmutable();" );

						} );

						writer.WriteLine( "}" );

					} );
					writer.WriteLine( "}" );

				} );
				writer.WriteLine( "}" );
			}

			return stringWriter.ToString();
		}
	}
}
