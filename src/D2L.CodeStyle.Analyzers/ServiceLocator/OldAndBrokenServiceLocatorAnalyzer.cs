using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class OldAndBrokenServiceLocatorAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.OldAndBrokenLocatorIsObsolete );

		private readonly bool _excludeKnownProblems;

		public OldAndBrokenServiceLocatorAnalyzer() : this(true) { }

		public OldAndBrokenServiceLocatorAnalyzer( bool excludeKnownProblemDlls ) {
			_excludeKnownProblems = excludeKnownProblemDlls;
		}

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterServiceLocatorAnalyzer );
		}

		public void RegisterServiceLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocator" );
			var factoryType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocatorFactory" );
			var activatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.IObjectActivator" );
			var customActivatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.ICustomObjectActivator" );

			// If those type lookups failed then OldAndBrokenServiceLocator
			// cannot resolve and we don't need to register our analyzer.

			if( locatorType == null || locatorType.Kind == SymbolKind.ErrorType ) {
				return;
			}
			if ( factoryType == null || factoryType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( IsAssemblyWhitelisted( context.Compilation.AssemblyName ) ) {
				return;
			}

			//Prevent static usage of OldAndBrokenServiceLocator
			//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
			context.RegisterSyntaxNodeAction(
				ctx => PreventOldAndBrokenUsage(
					ctx,
					new List<INamedTypeSymbol> { locatorType, factoryType, activatorType, customActivatorType }
				),
				SyntaxKind.IdentifierName
			);
		}

		//Prevent static usage of OldAndBrokenServiceLocator
		//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
		private static void PreventOldAndBrokenUsage(
			SyntaxNodeAnalysisContext context,
			List<INamedTypeSymbol> disallowedTypes
		) {
			var actualType = context.SemanticModel.GetTypeInfo( context.Node ).Type as INamedTypeSymbol;
			
			if ( actualType == null ) {
				return;
			}

			if (disallowedTypes.Contains( actualType )) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, context.Node.GetLocation() )
				);
			}
		}


		/// <summary>
		/// A list of assemblies that already contain OldAndBrokenServiceLocator references
		/// </summary>
		private static readonly ImmutableHashSet<string> WhitelistedAssemblies = new HashSet<string> {
			"D2L",
			"D2L.AP.S3.SISImporter",
			"D2L.Awards",
			"D2L.Core.JobManagement",
			"D2L.Core.Metadata",
			"D2L.Core.ReleaseConditions",
			"D2L.Core.Users",
			"D2L.Core.Workflow",
			"D2L.Custom.BannerBatchCrosslist",
			"D2L.Custom.ContentLicenseCheck",
			"D2L.Custom.CopyCourseAfterHT",
			"D2L.Custom.CourseBrandingAPI",
			"D2L.Custom.CourseBrandingRunner",
			"D2L.Custom.DropboxArchiver",
			"D2L.Custom.DropboxArchiver.Core",
			"D2L.Custom.EPSharingGroups",
			"D2L.Custom.ExternalHomepageResolver",
			"D2L.Custom.GradingListAPI",
			"D2L.Custom.IPASDelimitedUserMapping",
			"D2L.Custom.IPASUserCreateUpdate",
			"D2L.Custom.MapleTA",
			"D2L.Custom.ParticipationReport",
			"D2L.Custom.PDGG",
			"D2L.Custom.PDGG.Core",
			"D2L.Custom.Platform",
			"D2L.Custom.RefererLogout",
			"D2L.Custom.SchoolDependentSelfReg.Webpages",
			"D2L.Custom.SessionCourseCopy",
			"D2L.Custom.SpecialAccessAPIs",
			"D2L.Custom.StudentOrientationSIS.Task",
			"D2L.Custom.UpdateSproc",
			"D2L.Custom.UrkundIntegration",
			"D2L.Custom.UserActivityRFReport",
			"D2L.Custom.UserSyncTool",
			"D2L.Custom.ValenceAPI.WorkspaceManagement",
			"D2L.eP.Domain",
			"D2L.eP.Forms",
			"D2L.eP.Forms.Implementation",
			"D2L.eP.Forms.Services",
			"D2L.eP.Forms.Web",
			"D2L.eP.ImportExport",
			"D2L.eP.Services",
			"D2L.eP.Web",
			"D2L.eP.Webpages",
			"D2L.Ext.RemotePlugins",
			"D2L.IM.GoogleApps.Drive",
			"D2L.IM.GradesExport.Banner",
			"D2L.IM.GradesExport.Domain",
			"D2L.IM.IPSCT",
			"D2L.IM.IPSCT.Domain",
			"D2L.IM.IPSCT.SyncService",
			"D2L.IM.IPSCT.WebEx",
			"D2L.IM.IPSIS.Default",
			"D2L.IM.IPSIS.LIS",
			"D2L.IM.IPSIS.Security",
			"D2L.IM.Office365",
			"D2L.IM.Office365.Webpages",
			"D2L.IM.Platform.Data",
			"D2L.IM.Platform.DI",
			"D2L.IM.Platform.Enrollments",
			"D2L.IM.Platform.OrgUnits",
			"D2L.IM.Platform.Users",
			"D2L.IM.Platform.Web.MVC",
			"D2L.Integration.LOR_LE.Web",
			"D2L.Integration.LOR_PT.Web",
			"D2L.LE.Accelerator",
			"D2L.LE.Authorization",
			"D2L.LE.Classlist",
			"D2L.LE.Competencies",
			"D2L.LE.Content",
			"D2L.LE.Conversion",
			"D2L.LE.Conversion.ClientDataConverters.BB",
			"D2L.LE.CopyCourse",
			"D2L.LE.Discussions",
			"D2L.LE.Dropbox",
			"D2L.LE.Grades",
			"D2L.LE.Lti",
			"D2L.LE.News",
			"D2L.LE.Pager",
			"D2L.LE.QuestionCollection",
			"D2L.LE.Quizzing",
			"D2L.LE.Schedule",
			"D2L.LE.Scorm",
			"D2L.LE.SeatingChart",
			"D2L.LE.Survey",
			"D2L.Lms.AuthorizationSchemes.CommonCartridge",
			"D2L.Lms.Checklist.Implementation",
			"D2L.Lms.Competencies",
			"D2L.Lms.Competencies.Implementation",
			"D2L.Lms.Competencies.Web",
			"D2L.Lms.Content.Implementation",
			"D2L.Lms.Conversion.Adaptor",
			"D2L.Lms.Conversion.CoursePackages",
			"D2L.Lms.CourseExport",
			"D2L.Lms.CourseImport",
			"D2L.Lms.Crosslistings.Implementation",
			"D2L.Lms.Discussions.Implementation",
			"D2L.Lms.Discussions.Web",
			"D2L.Lms.Domain",
			"D2L.Lms.Domain.DataAccess",
			"D2L.Lms.Dropbox.Implementation",
			"D2L.Lms.Dropbox.Integration",
			"D2L.Lms.Dropbox.TurnItIn",
			"D2L.Lms.Dropbox.Web",
			"D2L.Lms.Email",
			"D2L.Lms.Email.Implementation",
			"D2L.Lms.Grades.Implementation",
			"D2L.Lms.Grades.ImportExport",
			"D2L.Lms.Grades.Web",
			"D2L.Lms.News.Implementation",
			"D2L.Lms.Question",
			"D2L.Lms.QuestionCollection.Implementation",
			"D2L.Lms.Quizzing.Implementation",
			"D2L.Lms.Quizzing.Web",
			"D2L.Lms.Quizzing.Webpages",
			"D2L.Lms.Scorm.Implementation",
			"D2L.Lms.Survey.Implementation",
			"D2L.Lms.Survey.Web",
			"D2L.Lms.Survey.Webpages",
			"D2L.Lms.Widgets.Implementation",
			"D2L.LOR",
			"D2L.LOR.FileSystemMigrator",
			"D2L.LOR.Import",
			"D2L.LOR.Legacy",
			"D2L.LOR.LORBulkUpload",
			"D2L.LOR.Metadata",
			"D2L.LOR.Provider",
			"D2L.LOR.Web",
			"D2L.LP",
			"D2L.LP.AppLoader",
			"D2L.LP.Diagnostics.Console",
			"D2L.LP.Diagnostics.Tracing",
			"D2L.LP.Diagnostics.Web",
			"D2L.LP.Files.MoreDirectoryMigrator",
			"D2L.LP.Services.Framework",
			"D2L.LP.Tools",
			"D2L.LP.Tools.DataPurgeArchive",
			"D2L.LP.Tools.DataPurgeJobAdministration",
			"D2L.LP.Tools.Extensibility",
			"D2L.LP.Web",
			"D2L.LP.Web.ContentHandling",
			"D2L.LP.Web.UI",
			"D2L.LP.WebDAV.Security.Service",
			"D2L.PlatformTools.DataPurgeArchive",
			"D2L.PlatformTools.ErrorPages",
			"D2L.PlatformTools.Homepage",
			"D2L.PlatformTools.Navbars",
			"D2L.PlatformTools.SystemHealth",
			"D2L.Web",
			"D2L.Web.Common.Web",
			"OrgUnitXMLTool",
			"SampleExporter",
			"SampleImporter",
		}.ToImmutableHashSet();

		private bool IsAssemblyWhitelisted( string assemblyName ) {
			return _excludeKnownProblems && WhitelistedAssemblies.Contains( assemblyName );
		}
	}
}