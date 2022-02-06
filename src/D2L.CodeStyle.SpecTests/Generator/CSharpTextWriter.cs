using System.CodeDom.Compiler;

namespace D2L.CodeStyle.SpecTests.Generator {

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

	}
}
