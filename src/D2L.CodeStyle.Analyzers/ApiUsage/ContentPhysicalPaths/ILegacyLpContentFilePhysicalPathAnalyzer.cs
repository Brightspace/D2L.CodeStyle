using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ContentPhysicalPaths {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ILegacyLpContentFilePhysicalPathAnalyzer : DiagnosticAnalyzer {

		private const string TypeName = "D2L.Files.ILegacyLpContentFile";
		private const string PropertyName = "PhysicalPath";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				PhysicalPathPropertyAnalysis.DiagnosticDescriptor
			);

		public override void Initialize( AnalysisContext context ) {

			PhysicalPathPropertyAnalysis analysis = new PhysicalPathPropertyAnalysis(
					TypeName,
					PropertyName,
					WhitelistedTypes
				);

			analysis.Initialize( context );
		}

		/// <summary>
		/// A list of types that already contain ILegacyLpContentFile.PhysicalPath references
		/// </summary>
		private static readonly IImmutableSet<string> WhitelistedTypes = ImmutableHashSet.Create<string>(
				"D2L.Files.ContentFileWrapper",
				"D2L.Files.IFileExtensions",
				"D2L.Files.IFileExtensions.ContentFileAdapter",
				"D2L.PlatformTools.ManageFiles.BusinessLayer.Domain.FileSystemManager"
			);
	}
}
