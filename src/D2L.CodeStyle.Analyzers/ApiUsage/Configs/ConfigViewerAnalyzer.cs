using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Configs {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ConfigViewerAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.BannedConfig
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterConfigViewerAnalysis );
		}

		private void RegisterConfigViewerAnalysis( CompilationStartAnalysisContext context ) {
			var IConfigViewer = context.Compilation.GetTypeByMetadataName( "D2L.LP.Configuration.Config.Domain.IConfigViewer" );

			// IConfigViewer not in compilation, no need to register
			if( IConfigViewer == null || IConfigViewer.Kind == SymbolKind.ErrorType ) {
				return;
			}

			var bannedConfigs = GetBannedConfigs( IConfigViewer );

			if( bannedConfigs.Count == 0 ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => ConfigViewerInvocationAnalysis(
					ctx,
					IConfigViewer,
					bannedConfigs,
					ctx.Node as InvocationExpressionSyntax
				),
				SyntaxKind.InvocationExpression
			);
		}

		private void ConfigViewerInvocationAnalysis(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol IConfigViewer,
			IReadOnlyDictionary<ISymbol, IReadOnlyDictionary<string, string>> bannedConfigs,
			InvocationExpressionSyntax invocationSyntax
		) {
			SemanticModel model = context.SemanticModel;

			ISymbol methodSymbol = model.GetSymbolInfo( invocationSyntax.Expression ).Symbol;

			if( methodSymbol == null || methodSymbol.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( !bannedConfigs.TryGetValue(
				methodSymbol.OriginalDefinition,
				out IReadOnlyDictionary<string, string> messages
			) ) {
				return;
			}

			if( !TryGetConfigNameArgumentFromInvocation(
				model,
				invocationSyntax,
				out ExpressionSyntax configNameArg
			) ) {
				return;
			}

			if( !TryGetConfigNameFromInvocation(
				model,
				configNameArg,
				out string configName
			) ) {
				return;
			}

			if( !messages.TryGetValue( configName, out string message ) ) {
				return;
			}

			string canonicalConfigName = messages.Keys.First( k => k.Equals( configName, StringComparison.OrdinalIgnoreCase ) );

			ReportDiagnostic(
				context: context,
				configNameArg: configNameArg,
				configName: canonicalConfigName,
				deprecationMessage: message
			);
		}

		private bool TryGetConfigNameArgumentFromInvocation(
			SemanticModel model,
			InvocationExpressionSyntax invocationSyntax,
			out ExpressionSyntax configNameArg
		) {
			foreach( ArgumentSyntax arg in invocationSyntax.ArgumentList.Arguments ) {
				IParameterSymbol parameter = arg.DetermineParameter( model );

				if( parameter.Name == "configName" ) {
					configNameArg = arg.Expression;
					return true;
				}
			}

			configNameArg = null;
			return false;
		}

		private bool TryGetConfigNameFromInvocation(
			SemanticModel model,
			ExpressionSyntax configNameArg,
			out string configName
		) {
			Optional<object> maybeConfigName = model.GetConstantValue( configNameArg );
			if( !maybeConfigName.HasValue ) {
				configName = null;
				return false;
			}

			configName = ( string )maybeConfigName.Value;
			return true;
		}

		private void ReportDiagnostic(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax configNameArg,
			string configName,
			string deprecationMessage
		) {
			context.ReportDiagnostic( Diagnostic.Create(
				Diagnostics.BannedConfig,
				configNameArg.GetLocation(),
				configName,
				deprecationMessage
			) );
		}

		private IReadOnlyDictionary<ISymbol, IReadOnlyDictionary<string, string>> GetBannedConfigs(
			INamedTypeSymbol IConfigViewer
		) {
			var builder = ImmutableDictionary.CreateBuilder<
				ISymbol,
				IReadOnlyDictionary<string, string>
			>();

			foreach( var definition in BannedConfigs.Definitions ) {
				string methodName = definition.Key;

				var methods = IConfigViewer
					.GetMembers( methodName )
					.Where( m => m.Kind == SymbolKind.Method );

				foreach( ISymbol method in methods ) {
					builder.Add( method, definition.Value );
				}
			}

			return builder.ToImmutable();
		}

	}
}
