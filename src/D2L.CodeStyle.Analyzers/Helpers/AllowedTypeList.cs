using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.Helpers {

	internal sealed class AllowedTypeList {

		public static AllowedTypeList CreateFromAnalyzerOptions(
			string allowedListFileName,
			AnalyzerOptions analyzerOptions
		) {
			return new AllowedTypeList(
				allowedListFileName: allowedListFileName,
				allowedList: LoadAllowedList( allowedListFileName, analyzerOptions.AdditionalFiles )
			);
		}

		private readonly string m_allowedListFileName;
		private readonly ImmutableHashSet<string> m_allowedList;

		private AllowedTypeList(
			string allowedListFileName,
			ImmutableHashSet<string> allowedList
		) {
			m_allowedListFileName = allowedListFileName;
			m_allowedList = allowedList;
		}

		public bool Contains( INamedTypeSymbol entry ) {
			string entryString = FormatEntry( entry );
			bool contains = m_allowedList.Contains( entryString );
			return contains;
		}

		public delegate void ReportDiagnostic( Diagnostic diagnostic );
		public void ReportEntryAsUnnecesary(
			INamedTypeSymbol entry,
			Location location,
			ReportDiagnostic report
		) {
			report( Diagnostic.Create(
				descriptor: Diagnostics.UnnecessaryAllowedListEntry,
				location: location,
				FormatEntry( entry ),
				m_allowedListFileName
			) );
		}

		private static string FormatEntry( INamedTypeSymbol entry ) {
			return $"{ entry.ToString() }, { entry.ContainingAssembly.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) }";
		}

		private static ImmutableHashSet<string> LoadAllowedList(
			string allowedListFileName,
			ImmutableArray<AdditionalText> additionalFiles
		) {
			ImmutableHashSet<string>.Builder allowedList = ImmutableHashSet.CreateBuilder(
				StringComparer.Ordinal
			);

			AdditionalText allowedListFile = additionalFiles.FirstOrDefault(
				file => Path.GetFileName( file.Path ) == allowedListFileName
			);

			if( allowedListFile == null ) {
				return allowedList.ToImmutableHashSet();
			}

			SourceText allowedListText = allowedListFile.GetText();

			foreach( TextLine line in allowedListText.Lines ) {
				allowedList.Add( line.ToString().Trim() );
			}

			return allowedList.ToImmutableHashSet();
		}

	}
}
