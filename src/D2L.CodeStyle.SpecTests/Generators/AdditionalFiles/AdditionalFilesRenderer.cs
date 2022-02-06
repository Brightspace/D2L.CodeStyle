namespace D2L.CodeStyle.SpecTests.Generators.AdditionalFiles {

	internal static class AdditionalFilesRenderer {

		public sealed record AdditionalFile(
			string IncludePath,
			string Text,
			string VirtualPath
		);

		public static string Render( IEnumerable<AdditionalFile> additionalFiles ) {

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

								writer.WriteEmptyLine();
								writer.WriteLine( "builder.Add( new AdditionalTextFile(" );
								writer.IndentBlock( () => {

									writer.Write( "path: " );
									writer.WriteString( additionalFile.VirtualPath );
									writer.WriteLine( "," );

									writer.Write( "text: " );
									writer.WriteMultiLineString( additionalFile.Text );
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
