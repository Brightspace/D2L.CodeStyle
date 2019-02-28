using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ContentPhysicalPaths {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ILpContentFilePhysicalPathAnalyzer : DiagnosticAnalyzer {

		private const string TypeName = "D2L.LP.Files.Domain.ILpContentFile";
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
		/// A list of types that already contain ILpContentFile.PhysicalPath references
		/// </summary>
		private static readonly IImmutableSet<string> WhitelistedTypes = ImmutableHashSet.Create<string>(
				"D2L.LP.Web.ContentHandling.Security.Default.ContentRequestChecker",
				"D2L.LE.Content.Extensibility.Service.Content.Default.ContentTopic.TopicService",
				"D2L.LE.ToolIntegration.Content.ResourceModifiers.Content.ContentFile",
				"D2L.LE.ToolIntegration.Content.TopicLocationResolvers.Controllers.EditFileController",
				"D2L.WCS.Integrations.Content.AddCapture.Utils.Embed.EmbedCaptureSourceUpdater",
				"D2L.Video.Integrations.Content.AddSubtitle.Utils.SubtitleManager",
				"D2L.Video.Integrations.Content.AddVideo.Utils.Embed.EmbedVideoSourceUpdater"
			);

	}
}
