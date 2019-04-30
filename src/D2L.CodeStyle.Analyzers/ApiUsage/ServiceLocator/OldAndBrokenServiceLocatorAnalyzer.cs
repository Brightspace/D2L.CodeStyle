﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {
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

			if( !disallowedTypes.Contains( actualType ) ) {
				return;
			}

			var parentClasses = context.Node.Ancestors().Where( a => a.IsKind( SyntaxKind.ClassDeclaration ) );
			var parentSymbols = parentClasses.Select( c => context.SemanticModel.GetDeclaredSymbol( c ) );
			if( parentSymbols.Any( s => Attributes.DIFramework.IsDefined( s ) ) ) {
				//Classes in the DI Framework are allowed to use locators and activators
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, context.Node.GetLocation() )
			);
		}


		/// <summary>
		/// A list of assemblies that already contain OldAndBrokenServiceLocator references
		/// </summary>
		private static readonly ImmutableHashSet<string> WhitelistedAssemblies = new HashSet<string> {
			"D2L",
			"D2L.Core.JobManagement",
			"D2L.Core.Metadata",
			"D2L.Core.ReleaseConditions",
			"D2L.Core.Workflow",
			"D2L.Custom.BannerBatchCrosslist",
			"D2L.Custom.ContentLicenseCheck",
			"D2L.Custom.ExternalHomepageResolver",
			"D2L.Custom.IPASDelimitedUserMapping",
			"D2L.Custom.IPASUserCreateUpdate",
			"D2L.Custom.ParticipationReport",
			"D2L.Custom.PDGG.Core",
			"D2L.Custom.SessionCourseCopy",
			"D2L.Custom.SpecialAccessAPIs",
			"D2L.Custom.StudentOrientationSIS.Task",
			"D2L.Custom.UserActivityRFReport",
			"D2L.Custom.UserSyncTool",
			"D2L.IM.GradesExport.Banner",
			"D2L.IM.GradesExport.Domain",
			"D2L.IM.GradesExport.Service",
			"D2L.IM.IPSCT",
			"D2L.IM.IPSCT.SyncService",
			"D2L.IM.IPSCT.WebEx",
			"D2L.IM.IPSIS.Default",
			"D2L.IM.IPSIS.LIS",
			"D2L.IM.IPSIS.Security",
			"D2L.IM.Platform.Data",
			"D2L.IM.Platform.DI",
			"D2L.IM.Platform.Enrollments",
			"D2L.IM.Platform.OrgUnits",
			"D2L.IM.Platform.Users",
			"D2L.IM.Platform.Web.MVC",
			"D2L.LE.Authorization",
			"D2L.LE.Competencies",
			"D2L.LE.Conversion.ClientDataConverters.BB",
			"D2L.LE.Discussions",
			"D2L.LE.Grades",
			"D2L.LE.Lti",
			"D2L.Lms.AuthorizationSchemes.CommonCartridge",
			"D2L.Lms.Competencies.Web",
			"D2L.Lms.Content.Implementation",
			"D2L.Lms.CourseExport",
			"D2L.Lms.Crosslistings.Implementation",
			"D2L.Lms.Discussions.Implementation",
			"D2L.Lms.Domain",
			"D2L.Lms.Domain.DataAccess",
			"D2L.Lms.Dropbox",
			"D2L.Lms.Dropbox.Integration",
			"D2L.Lms.Dropbox.Web",
			"D2L.Lms.Question",
			"D2L.Lms.Quizzing.Implementation",
			"D2L.LP",
			"D2L.LP.AppLoader",
			"D2L.LP.Diagnostics.Web",
			"D2L.LP.Services.Framework",
			"D2L.LP.Tools",
			"D2L.LP.Tools.Extensibility",
			"D2L.LP.Web",
			"D2L.LP.Web.UI",
			"D2L.PlatformTools.DataPurgeArchive",
			"D2L.PlatformTools.ErrorPages",
			"D2L.Web"
		}.ToImmutableHashSet();

		private bool IsAssemblyWhitelisted( string assemblyName ) {
			return _excludeKnownProblems && WhitelistedAssemblies.Contains( assemblyName );
		}
	}
}
