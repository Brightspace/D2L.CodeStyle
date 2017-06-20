using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class OldAndBrokenServiceLocatorAnalyzer : DiagnosticAnalyzer {
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

			context.RegisterSyntaxNodeAction(
				ctx => PreventStaticAccess(
					ctx,
					locatorType
				),
				SyntaxKind.IdentifierName
			);

			context.RegisterSyntaxNodeAction(
				ctx => PreventFactoryInstantiation(
					ctx,
					factoryType
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		//Prevent static usage of OldAndBrokenServiceLocator
		//For example, OldAndBrokenServiceLocator.Instance.Get<IFoo>()
		private static void PreventStaticAccess(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol locatorType
		) {
			var actualType = context.SemanticModel.GetTypeInfo( context.Node ).Type as INamedTypeSymbol;
			
			if ( actualType == null ) {
				return;
			}

			if (locatorType.Equals( actualType )) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, context.Node.GetLocation() )
				);
			}
		}

		//Prevent usage of the OldAndBrokenServiceLocatorFactory constructor
		private static void PreventFactoryInstantiation(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol factoryType
		) {
			var actualType = context.SemanticModel.GetTypeInfo( context.Node ).Type as INamedTypeSymbol;

			if( actualType == null ) {
				return;
			}

			if( factoryType.Equals( actualType ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.OldAndBrokenLocatorIsObsolete, context.Node.GetLocation() )
				);
			}
		}
	}
}