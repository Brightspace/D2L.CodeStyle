﻿using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly {
	internal static class LegacyILaunchDarklyClientConsumers {
		internal static readonly IImmutableSet<string> Types = ImmutableHashSet.Create<string>(
			"D2L.ClassStream.FeatureFlag.ClassStreamFeatureToggle.IsOneDrivePickerEnabled",
			"D2L.ClassStream.FeatureFlag.ClassStreamFeatureToggle.IsRegionEnabled",
			"D2L.ClassStream.FeatureFlag.ClassStreamFeatureToggle.UseRegionOverride",
			"D2L.ContentService.Feature.ContentServiceFeatureFlag.IsEnabled",
			"D2L.Custom.DataExport.Domain.Default.DataExportWorker.RunReport",
			"D2L.Custom.DataSets.Data.MsSql.DataSetDataProvider.FetchLearnerActivity",
			"D2L.Custom.VisionIntegration.Service.Default.CourseCompletionService.SendMessage",
			"D2L.Folio.Feature.FolioFeatureFlag.IsEnabled",
			"D2L.IM.IPSCT.WebPages.RoomDetails.ShowViewRoomDetails",
			"D2L.IM.IPSCT.WebPages.RoomList.D2LLoad",
			"D2L.IM.IPSIS.Admin.Console.PlugAndPlayUI.Controllers.AdminController.GetToggleState",
			"D2L.IM.IPSIS.Admin.Console.PlugAndPlayUI.Controllers.CredentialsController.GenerateNewSFTPPassword",
			"D2L.IM.IPSIS.Admin.Console.PlugAndPlayUI.Controllers.CredentialsController.GenerateSFTPCredentials",
			"D2L.IM.IPSIS.Admin.Console.PlugAndPlayUI.Controllers.CredentialsController.GetSFTPCredentials",
			"D2L.IM.IPSIS.Admin.Console.Security.LaunchDarklyOrgUnitPermission.HasPermission",
			"D2L.IM.IPSIS.Admin.Console.Web.Desktop.Views.Configuration.ExtensionPoints.ExtensionPointConfigFactory.CreatePartialView",
			"D2L.IM.IPSIS.Default.OrgUnits.BaseReplaceCleanseOrgUnitHandler.CleanseData",
			"D2L.IM.IPSIS.Default.OrgUnits.CourseOfferings.Handlers.DeleteCourseOfferingInactiveLEHandler.Process",
			"D2L.IM.IPSIS.Default.OrgUnits.CourseSections.Handlers.ReplaceCourseSectionCreateLMSSectionHandler.Process",
			"D2L.IM.IPSIS.Default.OrgUnits.CourseTemplates.Handlers.ReplaceCourseTemplateParentsNoUpdateLMSHandler.Process",
			"D2L.IM.IPSIS.Default.Service.QueueHandlerService.GetEnabledOrganizations",
			"D2L.Lang.Cache.AdaptiveLanguageCacheFactory.Create",
			"D2L.Lang.Cache.Data.SystemLanguageCacheProviderFactory.Create",
			"D2L.LE.Calendar.Domain.Copy.CalendarCopier.CopyEntryPerformanceFixFeatureFlag",
			"D2L.LE.Classlist.ReleaseConditions.FeatureToggle.ConditionalReleaseFilterSectionsByRolePropertyFeature.UseFeature",
			"D2L.LE.Competencies.Data.MsSql.CompetenciesCopyDataProvider.CopyAndConnectAllCompetencyComponents",
			"D2L.LE.Content.Web.Desktop.Controllers.Lessons.LessonsController.MakeAppOptionsForFullFra",
			"D2L.LE.Content.Web.Desktop.Views.ModuleDetails.ModuleContentBuilder.FetchCategories",
			"D2L.LE.CopyCourse.Domain.Default.ReleaseConditionsCopyProvider.CopyAllReleaseConditions",
			"D2L.LE.Core.FeatureToggle.GroupFilteredDiscussionTopicsPostToAllGroupsFeature.IsEnabled",
			"D2L.LE.Dropbox.FeatureToggles.DropboxPostsOnActivityFeed.IsDropboxPostsOnActivityFeedEnabled",
			"D2L.LE.Dropbox.FeatureToggles.QueryDropboxFoldersWithNoFilesFeature.UseFeature",
			"D2L.LE.Dropbox.FeatureToggles.QueryDropboxFolderUsersPerformanceFeatureFlag.IsImprovementEnabled",
			"D2L.LE.Email.Domain.Validation.Default.NumberAddressesValidator.IsValid",
			"D2L.LE.Grades.AssetManagement.GradesCopier.CopyAllItems",
			"D2L.LE.Grades.AssetManagement.GradesItemSelectionSource.IncludeFinalGrades",
			"D2L.LE.Grades.Integration.FeatureToggles.DeadMessageTestFeatureFlag.IsEnabled",
			"D2L.LE.Groups.FeatureToggles.GroupFilteredDiscussionTopicsFeature.IsEnabled",
			"D2L.LE.ManageCourses.Web.Desktop.Controllers.SearchController.Search",
			"D2L.LE.QuestionCollection.FeatureFlags.QEDConfigVariableFeatureFlag.IsEnabled",
			"D2L.LE.QuestionCollection.FeatureFlags.QIBLFeatureFlag.IsEnabled",
			"D2L.LE.QuestionCollection.FeatureFlags.QuestionImportFeatureFlag.IsEnabled",
			"D2L.LE.QuestionCollection.FeatureFlags.SectionsFeatureFlag.IsEnabled",
			"D2L.LE.QuestionCollection.FeatureFlags.ShuffleFeatureFlag.IsEnabled",
			"D2L.LE.SeatingChart.Web.Desktop.Default.SeatingChartButtonSplitReplacementSwitch.IsEnabled",
			"D2L.LE.ToolIntegration.Content.AddTopic.CreateActivities.Checklist.CreateChecklistController.CreateChecklist",
			"D2L.LE.ToolIntegration.Content.AddTopic.CreateActivities.Discussions.CreateTopicController.CreateTopic",
			"D2L.LE.ToolIntegration.Content.AddTopic.CreateActivities.Dropbox.CreateDropboxController.CreateDropbox",
			"D2L.LE.ToolIntegration.Content.AddTopic.CreateActivities.Quiz.CreateQuizController.CreateQuiz",
			"D2L.LE.ToolIntegration.Content.AddTopic.CreateActivities.Survey.CreateSurveyController.CreateSurvey",
			"D2L.Lms.Email.EmailResponsiveSwitch.IsEnabled",
			"D2L.Lms.Grades.Database_GradesProvider.GetOrgUnitGradeValuesLatestRowVersion",
			"D2L.Lms.QuestionCollection.F13314SendAssessmentEventGradedEventFlag.IsEnabled",
			"D2L.Lms.QuestionCollection.FeatureFixDe24874.IsEnabled",
			"D2L.Lms.Quizzing.Web.Flags.QuizConfirmationFlag.GetValue",
			"D2L.LP.Configuration.Aws.AwsCredentialValidationStateProvider.GetState",
			"D2L.LP.Diagnostics.Tracing.Delivery.Telegraf.TelegrafDeliverer.Deliver",
			"D2L.LP.Enrollments.FeatureToggles.SubGroupsFeature.IsEnabled",
			"D2L.LP.Files.Compression.Default.DecompressionConfig.GetCompressionRatioBufferBytes",
			"D2L.LP.Files.Compression.Default.DecompressionConfig.GetMaxCompressionRatio",
			"D2L.LP.Files.Compression.Default.DecompressionConfig.GetMaxFileBytes",
			"D2L.LP.Files.Compression.Default.DecompressionConfig.GetMaxTotalBytes",
			"D2L.LP.LaunchDarkly.FeatureFlagging.Default.LaunchDarklyFeature.IsEnabled",
			"D2L.LP.Users.Profiles.Security.Default.UserProfileSecurityContext.CanSeeProfileImage",
			"D2L.LP.Web.RequestLogging.Domain.Default.RequestLogger.LogRequest",
			"D2L.LP.Web.UI.AppDynamics.AppDynamicsFeatureFlags.IsAsync",
			"D2L.LP.Web.UI.AppDynamics.AppDynamicsFeatureFlags.IsEnabled",
			"D2L.LP.Web.UI.Common.Controls.FixFullscreenIframeSizeFeature.GetValue",
			"D2L.LP.Web.UI.Desktop.Controls.Accordion.NoJQuerySwitch.IsEnabled",
			"D2L.LP.Web.UI.Desktop.Controls.ScrollSpy.NoJQuerySwitch.IsEnabled",
			"D2L.LP.Web.UI.Desktop.MasterPages.DaylightDialogScrollingGlobalClientSideFlag.GetValue",
			"D2L.LP.Web.UI.Flags.Default.FetchCacheTokenLocalStorageGlobalClientSideFlag.GetValue",
			"D2L.LP.Web.UI.GoogleAnalytics.GoogleAnalyticsPageElementLoader.LoadPageElements",
			"D2L.LP.Web.UI.Polymer.WebComponentPolyfillEarlyJavaScriptBlock.LoadPolyfill",
			"D2L.LP.Web.UI.Polymer.WebComponentPolyfillEarlyJavaScriptBlock.TryGetBlock",
			"D2L.PlatformTools.DataPurgeArchive.Web.Desktop.Default.DataPurgeButtonSplitReplacementSwitch.IsEnabled",
			"D2L.PlatformTools.Navbars.Web.Desktop.Default.DaylightNavbarEditPageSwitch.IsEnabled",
			"D2L.PlatformTools.Navbars.Web.Desktop.Default.DaylightThemeEditPageSwitch.IsEnabled",
			"D2L.PlatformTools.Navbars.Web.Desktop.ImageBasedNavigationSwitch.IsAvailable",
			"D2L.PlatformTools.ToolManagement.Licensing.Domain.Default.LicenseManager.IsLicensingEnabled",
			"D2L.ScheduledTasks.FeatureFlag.ScheduledTaskFeatureFlagCheck.CreateUser",
			"D2L.ScheduledTasks.FeatureFlag.ScheduledTaskFeatureFlagCheck.IsEnabled",
			"D2L.ScheduledTasks.FeatureFlag.ScheduledTaskFeatureFlagTrack.AddUserAttribute",
			"D2L.Telegraph.BEF.BefPublisher.PublishBatchForOrgAsync",
			"D2L.Telegraph.ExternalQueue.DefPublisher.OrgIsEnabled",
			"D2L.Telegraph.HealthCheck.TelegraphServiceHealthProvider.GetServiceHealths",
			"D2L.Web.application.ValueShadowingExceptionFlag.IsEnabled",
			"D2L.Web.UI.FileSelector.D2LRender"
		);
	}
}
