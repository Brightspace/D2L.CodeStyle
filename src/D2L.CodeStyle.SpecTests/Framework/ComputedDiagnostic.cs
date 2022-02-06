using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed record ComputedDiagnostic(
		string? Alias,
		string Id,
		LinePositionSpan LinePosition,
		string Message
	) {

		/// <remarks>
		/// Formatting output for Visual Studio Test Explorer
		/// </remarks>
		public override string ToString() {

			using StringWriter sw = new StringWriter();
			using( IndentedTextWriter writer = new IndentedTextWriter( sw, tabString: "  " ) ) {
				writer.Indent = 3;

				writer.WriteLine( "{" );
				writer.Indent++;

				writer.Write( "Alias = " );
				writer.Write( Alias ?? "<<unknown>>" );
				writer.WriteLine( "," );

				writer.Write( "Id = " );
				writer.Write( Id );
				writer.WriteLine( "," );

				writer.Write( "LinePosition = " );
				writer.Write( LinePosition );
				writer.WriteLine( "," );

				writer.Write( "Message = " );
				writer.Write( Message );
				writer.WriteLine( "," );

				writer.Indent--;
				writer.WriteLine( "}" );
			}

			return sw.ToString();
		}
	}
}
