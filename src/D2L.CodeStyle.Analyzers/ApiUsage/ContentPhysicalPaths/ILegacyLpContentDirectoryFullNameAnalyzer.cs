﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ContentPhysicalPaths {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ILegacyLpContentDirectoryFullNameAnalyzer : DiagnosticAnalyzer {

		private const string TypeName = "D2L.Files.ILegacyLpContentDirectory";
		private const string PropertyName = "FullName";

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
		/// A list of types that already contain ILegacyLpContentDirectory.FullName references
		/// </summary>
		private static readonly IImmutableSet<string> WhitelistedTypes = ImmutableHashSet.Create<string>(
				"D2L.Files.Content.ContentDirectory",
				"D2L.Files.Compression.Archive",
				"D2L.Files.Content.ContentFile",
				"D2L.Files.FileSystemObjectNameUtility",
				"D2L.Files.FileSystemObjectWrapper",
				"D2L.PlatformTools.ManageFiles.BusinessLayer.Domain.DirectoryEntityInternal",
				"D2L.PlatformTools.ManageFiles.BusinessLayer.Domain.FileSystemManager",
				"D2L.PlatformTools.ManageFiles.Webpages.Isf.MyComputer",
				"D2L.Lms.CourseExport.CoursePackage",
				"D2L.Integration.LOR_LE.Webpages.CourseBuilder.Rpcs"
			);
	}
}
