using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Visibility {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class CorsHeaderAppenderUsageAnalyzer : DiagnosticAnalyzer {

		private const string CorsInterface = "D2L.LP.Web.Cors.ICorsHeaderAppender";
		private const string CorsClass = "D2L.LP.Web.Cors.CorsHeaderAppender";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.DangerousUsageOfCorsHeaderAppender
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(
				RegisterCorsHeadersUsageAnalyzer
			);
		}

		public void RegisterCorsHeadersUsageAnalyzer(
			CompilationStartAnalysisContext context
		) {
			INamedTypeSymbol interfaceType = context.Compilation
				.GetTypeByMetadataName( CorsInterface );

			if( !interfaceType.IsNullOrErrorType() ) {
				context.RegisterSyntaxNodeAction(
					ctx => PreventInjection(
						ctx,
						interfaceType
					),
					SyntaxKind.ConstructorDeclaration
				);
			}

			INamedTypeSymbol classType = context.Compilation
				.GetTypeByMetadataName( CorsClass );

			if( !classType.IsNullOrErrorType() ) {
				context.RegisterSyntaxNodeAction(
					ctx => PreventManualInstantiation(
						ctx,
						classType
					),
					SyntaxKind.ObjectCreationExpression
				);
			}

		}

		/// <summary>
		/// Prevent injection of the ICorsHttpHeaderHelper interface except in
		/// a specific whitelist of classes.
		/// </summary>
		private static void PreventInjection(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol interfaceType
		) {
			var constructor = context.Node as ConstructorDeclarationSyntax;
			if( constructor == null ) {
				return;
			}

			foreach( var parameter in constructor.ParameterList.Parameters ) {
				INamedTypeSymbol paramType = context.SemanticModel
					.GetTypeInfo( parameter.Type )
					.Type as INamedTypeSymbol;

				if( paramType == null || !paramType.Equals( interfaceType ) ) {
					return;
				}

				var parentClasses = context.Node
					.Ancestors()
					.Where( a => a.IsKind( SyntaxKind.ClassDeclaration ) );

				var parentSymbols = parentClasses.Select(
					c => context.SemanticModel.GetDeclaredSymbol( c )
				);

				if( parentSymbols.Any( IsClassWhitelisted ) ) {
					return;
				}

				context.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.DangerousUsageOfCorsHeaderAppender,
						parameter.GetLocation()
					)
				);
			}
		}

		/// <summary>
		/// We never want to allow manual instantiation of this object, so no
		/// whitelist here.
		/// </summary>
		private static void PreventManualInstantiation(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol classType
		) {
			var instantiation = context.Node as ObjectCreationExpressionSyntax;
			if( instantiation == null ) {
				return;
			}

			INamedTypeSymbol baseType = context.SemanticModel
				.GetTypeInfo( instantiation )
				.Type as INamedTypeSymbol;

			if( baseType == null || !baseType.Equals( classType ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.DangerousUsageOfCorsHeaderAppender,
					context.Node.GetLocation()
				)
			);
		}

		/// <summary>
		/// A list of classes that have been cleared to implement these headers
		/// </summary>
		private static readonly ImmutableHashSet<string> WhitelistedClasses = new HashSet<string> {
			"D2L.LP.Web.ContentHandling.Handlers.ContentHttpHandler",
			"D2L.LP.Web.ContentHandling.Mapping.ContentHttpHandlerMapper",
			"D2L.LP.Web.Files.FileViewing.StreamFileViewerResult",
			"D2L.LP.Web.Files.FileViewing.Default.StreamFileViewerResultFactory"
		}.ToImmutableHashSet();

		private static bool IsClassWhitelisted( ISymbol classSymbol ) {
			return WhitelistedClasses.Contains( classSymbol.ToString() );
		}
	}
}
