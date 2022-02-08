using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests {

	public sealed class AdditionalFile : AdditionalText {

		private readonly string m_text;

		public AdditionalFile(
			string path,
			string text
		) {
			Path = path;
			m_text = text;
		}

		public override string Path { get; }
		public override SourceText GetText( CancellationToken cancellationToken = default ) => SourceText.From( m_text, Encoding.UTF8 );
	}
}
