using System.CodeDom.Compiler;

namespace D2L.CodeStyle.SpecTests.Generators {

	internal sealed class CSharpTextWriter : IndentedTextWriter {

		public CSharpTextWriter( StringWriter writer )
			: base( writer, tabString: "\t" ) {

			writer.NewLine = "\r\n";
		}

		public void IndentBlock( Action block ) {
			Indent++;
			block();
			Indent--;
		}

		/// <summary>
		/// Writes an empty line without leading indent whitespace.
		/// </summary>
		public void WriteEmptyLine() {

			int indent = Indent;
			Indent = 0;
			WriteLine();
			Indent = indent;
		}

		public void WriteString( string value ) {

			Write( '"' );

			for( int i = 0; i < value.Length; i++ ) {

				char c = value[ i ];
				switch( c ) {

					case '"':
						Write( '\\' );
						Write( c );
						break;

					case '\n':
						Write( "\\n" );
						break;

					case '\r':
						Write( "\\r" );
						break;

					default:
						Write( c );
						break;
				}
			}

			Write( '"' );
		}

		public void WriteMultiLineString( string value ) {

			Write( "@\"" );

			for( int i = 0; i < value.Length; i++ ) {

				char c = value[ i ];
				if( c == '"' ) {
					Write( '"' );
				}

				Write( c );
			}

			Write( '"' );
		}
	}
}
