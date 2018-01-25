using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.LaunchDarkly {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ObsoleteLaunchDarklyApisAnalyzer : DiagnosticAnalyzer {

		private const string IFeatureFullName = "D2L.LP.LaunchDarkly.FeatureFlagging.IFeature";
		private const string ILaunchDarklyClientName = "D2L.LP.LaunchDarkly.ILaunchDarklyClient";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ObsoleteLaunchDarklyFramework,
				Diagnostics.ObsoleteILaunchDarklyClientClient
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol featureInterfaceSymbol = compilation.GetTypeByMetadataName( IFeatureFullName );
			if( !featureInterfaceSymbol.IsNullOrErrorType() ) {

				context.RegisterSyntaxNodeAction(
						c => AnalyzeSimpleBaseType( c, featureInterfaceSymbol ),
						SyntaxKind.SimpleBaseType
					);
			}

			IImmutableSet<ISymbol> bannedMethods;
			if( TryGetBannedMethods( compilation, out bannedMethods ) ) {

				context.RegisterSyntaxNodeAction(
						c => AnalyzeInvocationExpression( c, bannedMethods ),
						SyntaxKind.InvocationExpression
					);
			}
		}

		private void AnalyzeSimpleBaseType(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol featureInterfaceSymbol
			) {

			SimpleBaseTypeSyntax baseTypeSyntax = (SimpleBaseTypeSyntax)context.Node;
			SymbolInfo baseTypeSymbol = context.SemanticModel.GetSymbolInfo( baseTypeSyntax.Type );

			ISymbol baseSymbol = baseTypeSymbol.Symbol;
			if( baseSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !baseSymbol.Equals( featureInterfaceSymbol ) ) {
				return;
			}

			SyntaxNode classNode = baseTypeSyntax.Parent.Parent;

			ISymbol featureSymbol = context.SemanticModel.GetDeclaredSymbol( classNode );
			if( featureSymbol.IsNullOrErrorType() ) {
				return;
			}

			string featureName = featureSymbol.ToDisplayString();
			if( LegacyFeatureTypes.Types.Contains( featureName ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.ObsoleteLaunchDarklyFramework,
					baseTypeSyntax.GetLocation()
				);

			context.ReportDiagnostic( diagnostic );
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
