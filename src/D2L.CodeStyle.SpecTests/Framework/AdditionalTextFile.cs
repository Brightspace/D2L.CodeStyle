using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed class AdditionalTextFile : AdditionalText {

		private readonly string m_text;

		public AdditionalTextFile( string path, string text ) {
			Path = path;
			m_text = text;
		}

		public override string Path { get; }

		public override SourceText GetText( CancellationToken cancellationToken = default ) {
			return SourceText.From( m_text, Encoding.UTF8 );
		}
	}
}
