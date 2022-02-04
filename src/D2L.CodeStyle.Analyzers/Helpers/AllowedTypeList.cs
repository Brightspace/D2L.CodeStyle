#nullable disable

using System.Collections.Concurrent;
using System.Collections.Immutable;
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

		private readonly ConcurrentDictionary<INamedTypeSymbol, bool> m_used = new( SymbolEqualityComparer.Default );

		private AllowedTypeList(
			string allowedListFileName,
			ImmutableHashSet<string> allowedList
		) {
			m_allowedListFileName = allowedListFileName;
			m_allowedList = allowedList;
		}

		public bool Contains( INamedTypeSymbol entry ) {
			if( ContainsInternal( entry ) ) {
				m_used[ entry ] = true;
				return true;
			}

			return false;
		}

		public void CollectSymbolIfContained( SymbolAnalysisContext ctx ) {
			ISymbol symbol = ctx.Symbol;

			if( symbol.Kind != SymbolKind.NamedType ) {
				return;
			}

			INamedTypeSymbol entry = (INamedTypeSymbol)symbol;

			if( ContainsInternal( entry ) ) {
				m_used.TryAdd( entry, false );
			}
		}

		public void ReportUnnecessaryEntries(
			CompilationAnalysisContext ctx
		) {
			foreach( var kv in m_used ) {
				bool used = kv.Value;
				if( used ) {
					continue;
				}

				INamedTypeSymbol entry = kv.Key;
				if( entry.Locations.IsEmpty ) {
					continue;
				}

				ctx.ReportDiagnostic(
					descriptor: Diagnostics.UnnecessaryAllowedListEntry,
					location: entry.Locations[0],
					messageArgs: new[] {
						FormatEntry( entry ),
						m_allowedListFileName
					}
				);
			}
		}

		private bool ContainsInternal( INamedTypeSymbol entry ) {
			string entryString = FormatEntry( entry );
			bool contains = m_allowedList.Contains( entryString );
			return contains;
		}

		private static string FormatEntry( INamedTypeSymbol entry ) {
			return $"{ entry }, { entry.ContainingAssembly.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) }";
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
				return allowedList.ToImmutable();
			}

			SourceText allowedListText = allowedListFile.GetText();

			foreach( TextLine line in allowedListText.Lines ) {
				allowedList.Add( line.ToString().Trim() );
			}

			return allowedList.ToImmutable();
		}

	}
}
