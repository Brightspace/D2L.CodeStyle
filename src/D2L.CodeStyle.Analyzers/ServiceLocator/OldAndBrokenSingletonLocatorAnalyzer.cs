using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class OldAndBrokenSingletonLocatorAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.SingletonLocatorMisuse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenSingletonLocator" );

			// If this type lookup failed then OldAndBrokenSingletonLocator
			// cannot resolve and we don't need to register our analyzer.

			if( locatorType == null || locatorType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					locatorType
				),
				SyntaxKind.InvocationExpression
			);
		}

		//Enforce that OldAndBrokenSingletonLocator can only load actual Singletons
		private static void EnforceSingletonsOnly(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol singletonLocatorType
		) {
			var root = context.Node as InvocationExpressionSyntax;
			if( root == null ) {
				return;
			}
			var method = context.SemanticModel.GetSymbolInfo( root ).Symbol as IMethodSymbol;
			if( method == null ) {
				return;
			}

			if( singletonLocatorType != method.ContainingType ) {
				return;
			}

			if( !IsSingletonGet( method ) ) {
				return;
			}
			
			//It's ok as long as the attribute is present, error otherwise
			ITypeSymbol typeArg = method.TypeArguments.First();
			if ( Attributes.Singleton.IsDefined( typeArg ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create( Diagnostics.SingletonLocatorMisuse, context.Node.GetLocation(), typeArg.GetFullTypeName() )
			);
		}

		private static bool IsSingletonGet( IMethodSymbol method ) {
			return "Get".Equals( method.Name )
				&& method.IsGenericMethod
				&& method.TypeArguments.Length == 1;
		}

	}
}