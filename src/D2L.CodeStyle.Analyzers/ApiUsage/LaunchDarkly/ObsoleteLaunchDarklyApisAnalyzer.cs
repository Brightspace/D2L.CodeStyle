using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ObsoleteLaunchDarklyApisAnalyzer : DiagnosticAnalyzer {

		private const string ILaunchDarklyClientName = "D2L.LP.LaunchDarkly.ILaunchDarklyClient";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ObsoleteILaunchDarklyClientClient
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			IImmutableSet<ISymbol> bannedMethods;
			if( TryGetBannedMethods( compilation, out bannedMethods ) ) {

				context.RegisterSyntaxNodeAction(
						c => AnalyzeInvocationExpression( c, bannedMethods ),
						SyntaxKind.InvocationExpression
					);
			}
		}

		private void AnalyzeInvocationExpression(
				SyntaxNodeAnalysisContext context,
				IImmutableSet<ISymbol> bannedMethods
			) {

			InvocationExpressionSyntax invocation = ( context.Node as InvocationExpressionSyntax );
			if( invocation == null ) {
				return;
			}

			ISymbol methodSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression )
				.Symbol;

			if( methodSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !bannedMethods.Contains( methodSymbol.OriginalDefinition ) ) {
				return;
			}

			string methodName = context.ContainingSymbol.ToDisplayString( MethodDisplayFormat );
			if( LegacyILaunchDarklyClientConsumers.Types.Contains( methodName ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.ObsoleteILaunchDarklyClientClient,
					invocation.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static bool TryGetBannedMethods(
				Compilation compilation,
				out IImmutableSet<ISymbol> bannedMethods
			) {

			INamedTypeSymbol type = compilation.GetTypeByMetadataName( ILaunchDarklyClientName );
			if( type.IsNullOrErrorType() ) {
				bannedMethods = null;
				return false;
			}

			bannedMethods = type.GetMembers().ToImmutableHashSet();
			return true;
		}

		private static readonly SymbolDisplayFormat MethodDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

	}
}
