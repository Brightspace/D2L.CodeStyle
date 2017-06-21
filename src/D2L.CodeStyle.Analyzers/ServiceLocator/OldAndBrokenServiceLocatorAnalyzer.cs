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

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterServiceLocatorAnalyzer );
		}

		public static void RegisterServiceLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocator" );
			var factoryType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocatorFactory" );

			// If those type lookups failed then OldAndBrokenServiceLocator
			// cannot resolve and we don't need to register our analyzer.

			if( locatorType == null || locatorType.Kind == SymbolKind.ErrorType ) {
				return;
			}
			if ( factoryType == null || factoryType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			//Prevent static usage of OldAndBrokenServiceLocator
			//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
			context.RegisterSyntaxNodeAction(
				ctx => PreventOldAndBrokenUsage(
					ctx,
					locatorType
				),
				SyntaxKind.IdentifierName
			);

			//Prevent usage of the OldAndBrokenServiceLocatorFactory constructor
			context.RegisterSyntaxNodeAction(
				ctx => PreventOldAndBrokenUsage(
					ctx,
					factoryType
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		//Prevent static usage of OldAndBrokenServiceLocator
		//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
		private static void PreventOldAndBrokenUsage(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol disallowedType
		) {
			var actualType = context.SemanticModel.GetTypeInfo( context.Node ).Type as INamedTypeSymbol;
			
			if ( actualType == null ) {
				return;
			}

			if (disallowedType.Equals( actualType )) {
				if( IsAssemblyWhitelisted( context.Compilation.AssemblyName ) ) {
					return;
				}
				
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, context.Node.GetLocation() )
				);
			}
		}


		/// <summary>
		/// A list of assemblies that already contain OldAndBrokenServiceLocator references
		/// </summary>
		private static readonly ImmutableHashSet<string> WhitelistedAssemblies = new HashSet<string> {
			"D2L.AP.ETL.Cleanup.Initiator",
			"D2L.AP.DataExtraction.Initiator",
			"D2L.AP.InsightsPortal.WebMVC",
			"D2L.AP.InsightsPortal.Webpages",
			"D2L.AW.DataExport.BrightspaceDataSets.IntegrationTests",
			"D2L.AW.DataImporter.Command",
			"D2L.Achievements",
			"OrgUnitXMLTool",
			"D2L.Custom.UpdateSproc",
			"D2L.Custom.Platform",
			"D2L.Lms.Domain",
			"D2L.Lms.Domain.DataAccess",
			"D2L.DataWarehouse.Warehouse.Warehouser",
			"D2L.Ext.RemotePlugins",
			"D2L.IM.GoogleApps.Legacy.Plugin.FileArea",
			"D2L.IM.GoogleApps.WebPages",
			"D2L.IM.GoogleApps.WidgetPages",
			"D2L.HoldingTank.RealtimeTestHarness",
			"D2L.IM.GradesExport.Domain",
			"D2L.Lms.Crosslistings.Implementation",
			"D2L.IM.IPSIS.LIS",
			"D2L.IM.IPSIS.Default",
			"D2L.IM.Platform.Enrollments",
			"D2L.IM.Platform.Web.MVC",
			"D2L.IM.IPSCT.SyncService",
			"D2L.IM.IPSCT.WebEx",
			"D2L.LE.Accelerator",
			"D2L.Lms.Blog.Webpages",
			"D2L.Lms.Chat.Webpages",
			"D2L.LE.Classlist",
			"D2L.Lms.Classlist.Webpages",
			"D2L.Lms.Competencies.Implementation",
			"D2L.Lms.Competencies.Web",
			"D2L.LE.Content.LoadTests",
			"D2L.Lms.Content",
			"D2L.Lms.Content.Implementation",
			"D2L.Lms.Content.Web",
			"D2L.LE.CourseBuilder.Webpages",
			"D2L.LE.Discussions",
			"D2L.LE.Discussions.Tests",
			"D2L.Lms.Discussions.Webpages",
			"D2L.Lms.Dropbox.Implementation",
			"D2L.Lms.Dropbox.Web",
			"D2L.Lms.Dropbox.Webpages",
			"D2L.Lms.Email.Implementation",
			"D2L.LE.Grades",
			"D2L.Lms.Grades.Implementation",
			"D2L.Lms.Grades.Webpages",
			"D2L.Lms.Conversion.CopyCourseBulk",
			"BulkCourseCreate",
			"BulkCourseExport",
			"D2L.LE.Conversion.ClientDataConverters.BB",
			"D2L.LE.Conversion",
			"D2L.Lms.Conversion.Adaptor",
			"D2L.Lms.CourseExport",
			"D2L.Lms.CourseImport",
			"D2L.Lms.Conversion.Webpages",
			"D2L.LE.IntelligentAgents",
			"D2L.Lms.Locker.Webpages",
			"D2L.LE.Pager.Webpages",
			"D2L.LE.QuestionCollection",
			"D2L.Lms.QuestionCollection.Implementation",
			"D2L.Lms.QuestionCollection.Web",
			"D2L.Lms.QuestionCollection.Webpages",
			"D2L.Lms.Question",
			"D2L.LE.Quizzing",
			"D2L.LE.Quizzing.Tests",
			"D2L.Lms.Quizzing.Webpages",
			"D2L.LE.Schedule",
			"D2L.LE.Scorm",
			"D2L.LE.Scorm.Tests",
			"D2L.LE.SeatingChart",
			"D2L.Lms.SelfAssessment.Webpages",
			"D2L.LE.Survey",
			"D2L.Lms.Survey.Webpages",
			"D2L.Lms.Widgets.Implementation",
			"D2L.Lms.Widgets.Webpages",
			"D2L.LE.Authorization",
			"D2L.LOR.FileSystemMigrator",
			"D2L.LOR.Legacy",
			"LORBulkUpload",
			"D2L.LOR.Provider",
			"D2L.LOR.Web",
			"D2L.LOR.Webpages",
			"D2L",
			"D2L.Core.Metadata",
			"D2L.Core.ReleaseConditions",
			"D2L.Core.Workflow",
			"D2L.LP",
			"D2L.LP.Diagnostics.Tracing",
			"D2L.Core.Users",
			"D2L.Core.JobManagement",
			"D2L.LP.Tools.DataPurgeJobAdministration",
			"D2L.LP.Tools.Extensibility",
			"D2L.LP.Web.ContentHandling",
			"D2L.Web",
			"D2L.LP.Files.MoreDirectoryMigrator",
			"D2L.LP.Integration.Tests",
			"D2L.LP.WebDAV.Security.Service",
			"D2L.Video.Integrations",
			"D2L.IM.Office365",
			"D2L.IM.Office365.Webpages",
			"D2L.PlatformTools.DataPurgeArchive",
			"D2L.LP.Diagnostics.Console",
			"D2L.PlatformTools.ErrorPages",
			"D2L.PlatformTools.SystemHealth",
			"D2L.WS.Implementation.Rws",
			"D2L.AP.S3.SISImporter",
			"D2L.ScheduledTasks.Console",
			"D2L.ScheduledTasks.Runner",
			"D2L.SelfRegistration.Webpages",
			"D2L.Custom.StudentOrientationSIS.Task",
			"D2L.WCS.Webpages"
		}.ToImmutableHashSet();

		private static bool IsAssemblyWhitelisted( string assemblyName ) {
			return WhitelistedAssemblies.Contains( assemblyName );
		}
	}
}