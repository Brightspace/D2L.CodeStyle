using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Helpers;

internal sealed class ExemptSymbolsBuilder {

	private readonly Compilation m_compilation;
	private readonly AnalyzerOptions m_analyzerOptions;
	private readonly CancellationToken m_cancellationToken;

	private readonly ImmutableHashSet<ISymbol>.Builder m_exemptions = ImmutableHashSet.CreateBuilder<ISymbol>( SymbolEqualityComparer.Default );

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"MicrosoftCodeAnalysisPerformance",
		"RS1012:Start action has no registered actions",
		Justification = "Not an analyzer"
	)]
	public ExemptSymbolsBuilder(
		CompilationStartAnalysisContext context
	) {
		m_compilation = context.Compilation;
		m_analyzerOptions = context.Options;
		m_cancellationToken = context.CancellationToken;
	}

	public ImmutableHashSet<ISymbol> Build() => m_exemptions.ToImmutable();

	/// <summary>
	/// Loads exemptions from AdditionalFiles matching "<paramref name="fileNameBase"/>.txt" and "<paramref name="fileNameBase"/>.*.txt".
	/// Each file should contain a series of lines, each starting with a DocumentationCommentDelcarationId, with an optional trailing comment (//).
	/// </summary>
	public ExemptSymbolsBuilder AddFromAdditionalFiles(
		string fileNameBase
	) {
		fileNameBase += ".";

		foreach( AdditionalText file in m_analyzerOptions.AdditionalFiles ) {
			string fileName = Path.GetFileName( file.Path );
			if( fileName is null ) {
				continue;
			}

			bool fileNameMatch = fileName.StartsWith( fileNameBase, StringComparison.Ordinal ) && fileName.EndsWith( ".txt", StringComparison.Ordinal );
			if( !fileNameMatch ) {
				continue;
			}

			SourceText? sourceText = file.GetText( m_cancellationToken );
			if( sourceText is null ) {
				continue;
			}

			foreach( TextLine line in sourceText.Lines ) {
				ReadOnlySpan<char> text = line.ToString().AsSpan();

				int commentIndex = text.IndexOf( "//".AsSpan(), StringComparison.Ordinal );
				if( commentIndex != -1 ) {
					text = text.Slice( 0, commentIndex );
				}

				text = text.TrimEnd();

				if( text.Length > 0 ) {
					AddFromDocumentationCommentId( text.ToString() );
				}
			}
		}

		return this;
	}

	public ExemptSymbolsBuilder AddFromDocumentationCommentId( string id ) {
		ImmutableArray<ISymbol> symbols = DocumentationCommentId.GetSymbolsForDeclarationId( id, m_compilation );
		foreach( var symbol in symbols ) {
			m_exemptions.Add( symbol );
		}

		return this;
	}

}
