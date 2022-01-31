#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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

			context.RegisterOperationAction(
				ctx => ConfigViewerInvocationAnalysis(
					ctx,
					bannedConfigs,
					ctx.Operation as IInvocationOperation
				),
				OperationKind.Invocation
			);
		}

		private void ConfigViewerInvocationAnalysis(
			OperationAnalysisContext context,
			IReadOnlyDictionary<IMethodSymbol, IReadOnlyDictionary<string, string>> bannedConfigs,
			IInvocationOperation invocationOperation
		) {
			if( !bannedConfigs.TryGetValue(
				invocationOperation.TargetMethod.OriginalDefinition,
				out IReadOnlyDictionary<string, string> messages
			) ) {
				return;
			}

			if( !TryGetConfigNameArgumentFromInvocation(
				invocationOperation,
				out IOperation configNameArg
			) ) {
				return;
			}

			if( !TryGetConfigNameFromInvocation(
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
			IInvocationOperation invocationSyntax,
			out IOperation configNameArg
		) {
			foreach( IArgumentOperation arg in invocationSyntax.Arguments ) {
				if( arg.Parameter.Name == "configName" ) {
					configNameArg = arg.Value;
					return true;
				}
			}

			configNameArg = null;
			return false;
		}

		private bool TryGetConfigNameFromInvocation(
			IOperation configNameArg,
			out string configName
		) {
			Optional<object> maybeConfigName = configNameArg.ConstantValue;
			if( !maybeConfigName.HasValue ) {
				configName = null;
				return false;
			}

			configName = ( string )maybeConfigName.Value;
			return true;
		}

		private void ReportDiagnostic(
			OperationAnalysisContext context,
			IOperation configNameArg,
			string configName,
			string deprecationMessage
		) {
			context.ReportDiagnostic( Diagnostic.Create(
				Diagnostics.BannedConfig,
				configNameArg.Syntax.GetLocation(),
				configName,
				deprecationMessage
			) );
		}

		private IReadOnlyDictionary<IMethodSymbol, IReadOnlyDictionary<string, string>> GetBannedConfigs(
			INamedTypeSymbol IConfigViewer
		) {
			var builder = ImmutableDictionary.CreateBuilder<
				IMethodSymbol,
				IReadOnlyDictionary<string, string>
			>( SymbolEqualityComparer.Default );

			foreach( var definition in BannedConfigs.Definitions ) {
				string methodName = definition.Key;

				var methods = IConfigViewer
					.GetMembers( methodName )
					.OfType<IMethodSymbol>();

				foreach( IMethodSymbol method in methods ) {
					builder.Add( method, definition.Value );
				}
			}

			return builder.ToImmutable();
		}

	}
}
