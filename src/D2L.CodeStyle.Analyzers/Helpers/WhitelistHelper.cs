using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Helpers {

	internal static class WhitelistHelper {

		public static WhitelistHelper<INamedTypeSymbol> TypeLevel(
			string whitelistFileName
		) => new WhitelistHelper<INamedTypeSymbol>(
			whitelistFileName: whitelistFileName,
			entryFormatter: s => $"{ s.ToString() }, { s.ContainingAssembly.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) }"
		);

	}

	internal sealed class WhitelistHelper<T> {

		public WhitelistHelper(
			string whitelistFileName,
			Func<T, string> entryFormatter
		) {
			WhitelistFileName = whitelistFileName;
			EntryFormatter = entryFormatter;
		}

		public string WhitelistFileName { get; }
		public Func<T, string> EntryFormatter { get; }

		public Func<T, bool> LoadWhitelist(
			AnalyzerOptions options
		) {
			ImmutableHashSet<string> whitelist = LoadWhitelist( options.AdditionalFiles );
			return IsInWhitelist;

			bool IsInWhitelist( T candidate ) {
				string entry = EntryFormatter( candidate );
				bool whitelisted = whitelist.Contains( entry );
				return whitelisted;
			}
		}

		public delegate void ReportDiagnostic( Diagnostic diagnostic );
		public void EntryIsUnnecessary( T entry, Location location, ReportDiagnostic report ) {
			report( Diagnostic.Create(
				descriptor: Diagnostics.UnnecessaryWhitelistEntry,
				location: location,
				EntryFormatter( entry ),
				WhitelistFileName
			) );
		}

		private ImmutableHashSet<string> LoadWhitelist(
			ImmutableArray<AdditionalText> additionalFiles
		) {
			ImmutableHashSet<string>.Builder whitelist = ImmutableHashSet.CreateBuilder(
				StringComparer.Ordinal
			);

			AdditionalText whitelistFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == WhitelistFileName
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
