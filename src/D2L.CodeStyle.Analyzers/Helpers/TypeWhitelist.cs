using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Helpers {

	internal sealed class TypeWhitelist {

		public static TypeWhitelist CreateFromAnalyzerOptions(
			string whitelistFileName,
			AnalyzerOptions analyzerOptions
		) {
			return new TypeWhitelist(
				whitelistFileName: whitelistFileName,
				whitelist: LoadWhitelist( whitelistFileName, analyzerOptions.AdditionalFiles )
			);
		}

		private readonly string m_whitelistFileName;
		private readonly ImmutableHashSet<string> m_whitelist;

		private TypeWhitelist(
			string whitelistFileName,
			ImmutableHashSet<string> whitelist
		) {
			m_whitelistFileName = whitelistFileName;
			m_whitelist = whitelist;
		}

		public bool Contains( INamedTypeSymbol entry ) {
			string entryString = FormatEntry( entry );
			bool contains = m_whitelist.Contains( entryString );
			return contains;
		}

		public delegate void ReportDiagnostic( Diagnostic diagnostic );
		public void ReportEntryAsUnnecesary(
			INamedTypeSymbol entry,
			Location location,
			ReportDiagnostic report
		) {
			report( Diagnostic.Create(
				descriptor: Diagnostics.UnnecessaryWhitelistEntry,
				location: location,
				FormatEntry( entry ),
				m_whitelistFileName
			) );
		}

		private static string FormatEntry( INamedTypeSymbol entry ) {
			return $"{ entry.ToString() }, { entry.ContainingAssembly.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) }";
		}

		private static ImmutableHashSet<string> LoadWhitelist(
			string whitelistFileName,
			ImmutableArray<AdditionalText> additionalFiles
		) {
			ImmutableHashSet<string>.Builder whitelist = ImmutableHashSet.CreateBuilder(
				StringComparer.Ordinal
			);

			AdditionalText whitelistFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == whitelistFileName
			);

			if( whitelistFile == null ) {
				return whitelist.ToImmutableHashSet();
			}

			SourceText whitelistText = whitelistFile.GetText();

			foreach( TextLine line in whitelistText.Lines ) {
				whitelist.Add( line.ToString().Trim() );
			}

			return whitelist.ToImmutableHashSet();
		}

	}
}
